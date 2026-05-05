import { useState } from 'react';
import {
  Stack, Title, Text, Tabs, Paper, Table, Group, Button, ActionIcon, Modal, TextInput, Pagination,
  Badge, Tooltip, ScrollArea, Select, Card, SimpleGrid, ThemeIcon, Skeleton, Box, Alert,
} from '@mantine/core';
import { CollapsibleFilters } from '../components/common/CollapsibleFilters';
import { useForm } from '@mantine/form';
import { useDisclosure } from '@mantine/hooks';
import { notifications } from '@mantine/notifications';
import {
  IconPencil,
  IconPlus,
  IconTrash,
  IconShield,
  IconUsers,
  IconKey,
  IconHistory,
  IconUserQuestion,
  IconLinkOff,
  IconShieldCheck,
  IconAlertCircle,
  IconSearch,
  IconX,
  IconInfoCircle,
} from '@tabler/icons-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getUsers, createUser, updateUser, deleteUser, type AppUser, type CreateUserDto } from '../api/users';
import {
  getAccounts,
  createAccount,
  updateAccount,
  deleteAccount,
  type AccountRow,
  type CreateAccountDto,
  type UpdateAccountDto,
} from '../api/accountsAdmin';
import { PageSizeSelect } from '../components/common/PageSizeSelect';
import { AdminAccountRightsModal } from '../components/admin/AdminAccountRightsModal';
import { AdminAccountActivityDrawer } from '../components/admin/AdminAccountActivityDrawer';
import { PasswordRequirementsModal } from '../components/admin/PasswordRequirementsModal';
import { PhoneRequirementsModal } from '../components/common/PhoneRequirementsModal';
import { EmailRequirementsModal } from '../components/common/EmailRequirementsModal';
import { getAdminMetrics } from '../api/adminMetrics';
import { fmtDateTime } from '../utils/format';
import {
  DbMax,
  combine,
  emailField,
  maxLen,
  optMaxLen,
  phoneField,
  required,
} from '../utils/fieldValidation';
import { requiredPassword, optionalPassword } from '../utils/passwordPolicy';

type AdminUserFilters = {
  surname: string;
  name: string;
  middleName: string;
  email: string;
  phone: string;
  position: string;
};

const EMPTY_USER_FILTERS: AdminUserFilters = {
  surname: '',
  name: '',
  middleName: '',
  email: '',
  phone: '',
  position: '',
};

function buildUserFilters(v: AdminUserFilters): Record<string, string> {
  const f: Record<string, string> = {};
  if (v.surname.trim()) f.Surname_contains = v.surname.trim();
  if (v.name.trim()) f.Name_contains = v.name.trim();
  if (v.middleName.trim()) f.MiddleName_contains = v.middleName.trim();
  if (v.email.trim()) f.Email_contains = v.email.trim();
  if (v.phone.trim()) f.PhoneNumber_contains = v.phone.trim();
  if (v.position.trim()) f.Position_contains = v.position.trim();
  return f;
}

type AdminAccountFilters = {
  login: string;
  description: string;
  userLink: 'all' | 'linked' | 'unlinked';
};

const EMPTY_ACCOUNT_FILTERS: AdminAccountFilters = {
  login: '',
  description: '',
  userLink: 'all',
};

/** Только поля, которые уходят в Filters (привязка — отдельным query userLink на API). */
function buildAccountFilters(v: AdminAccountFilters): Record<string, string> {
  const f: Record<string, string> = {};
  if (v.login.trim()) f.Login_contains = v.login.trim();
  if (v.description.trim()) f.Description_contains = v.description.trim();
  return f;
}

function AdminMetricsStrip() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['admin-metrics'],
    queryFn: getAdminMetrics,
  });

  const cards = [
    {
      key: 'users',
      label: 'Пользователей',
      value: data?.usersTotal,
      icon: IconUsers,
      color: 'indigo',
    },
    {
      key: 'accounts',
      label: 'Аккаунтов',
      value: data?.accountsTotal,
      icon: IconKey,
      color: 'violet',
    },
    {
      key: 'orphanAcc',
      label: 'Аккаунтов без профиля',
      value: data?.accountsWithoutUserProfile,
      icon: IconLinkOff,
      color: 'orange',
    },
    {
      key: 'orphanUser',
      label: 'Профилей без входа',
      value: data?.usersWithoutAccount,
      icon: IconUserQuestion,
      color: 'yellow',
    },
    {
      key: 'rights',
      label: 'Прав в справочнике',
      value: data?.rightsTotal,
      icon: IconShieldCheck,
      color: 'teal',
    },
  ] as const;

  return (
    <Box>
      {error && (
        <Alert icon={<IconAlertCircle size={16} />} color="red" mb="md" radius="md">
          Метрики не загрузились: {(error as Error).message}
        </Alert>
      )}
      <SimpleGrid cols={{ base: 1, xs: 2, sm: 3, lg: 5 }} spacing="md">
        {cards.map(({ key, label, value, icon: Icon, color }) => (
          <Card key={key} withBorder radius="md" padding="md">
            <Group justify="space-between" mb="xs" wrap="nowrap">
              <ThemeIcon size={40} radius="md" color={color} variant="light">
                <Icon size={20} />
              </ThemeIcon>
              <Text size="xs" c="dimmed" tt="uppercase" fw={600} ta="right" style={{ lineHeight: 1.25 }}>
                {label}
              </Text>
            </Group>
            {isLoading ? (
              <Skeleton height={32} width={72} mt={4} />
            ) : (
              <Text size="xl" fw={700}>
                {(value ?? 0).toLocaleString('ru-RU')}
              </Text>
            )}
          </Card>
        ))}
      </SimpleGrid>
    </Box>
  );
}

function UsersTab() {
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [pendingUserFilters, setPendingUserFilters] = useState<AdminUserFilters>(EMPTY_USER_FILTERS);
  const [appliedUserFilters, setAppliedUserFilters] = useState<Record<string, string>>({});
  const [editUser, setEditUser] = useState<AppUser | null>(null);
  const [createOpened, { open: openCreate, close: closeCreate }] = useDisclosure(false);
  const [deleteUserObj, setDeleteUserObj] = useState<AppUser | null>(null);
  const [emailHintOpened, { open: openEmailHint, close: closeEmailHint }] = useDisclosure(false);
  const [phoneHintOpened, { open: openPhoneHint, close: closePhoneHint }] = useDisclosure(false);

  const activeUserFiltersCount = Object.keys(appliedUserFilters).length;
  const hasUserFilters = activeUserFiltersCount > 0;

  const applyUserFilters = () => {
    setAppliedUserFilters(buildUserFilters(pendingUserFilters));
    setPage(1);
  };

  const resetUserFilters = () => {
    setPendingUserFilters(EMPTY_USER_FILTERS);
    setAppliedUserFilters({});
    setPage(1);
  };

  const onUserFilterKey = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') applyUserFilters();
  };

  const { data, isLoading, error } = useQuery({
    queryKey: ['admin-users', page, pageSize, appliedUserFilters],
    queryFn: () =>
      getUsers({
        pageNumber: page,
        pageSize,
        Filters: hasUserFilters ? appliedUserFilters : undefined,
      }),
  });

  const userFormValidate = {
    name: combine(required('Укажите имя'), maxLen(DbMax.user.name, 'Имя')),
    surname: combine(required('Укажите фамилию'), maxLen(DbMax.user.surname, 'Фамилия')),
    middleName: optMaxLen(DbMax.user.middleName, 'Отчество'),
    email: emailField('Email'),
    phoneNumber: phoneField('Телефон'),
    position: combine(required('Укажите должность'), maxLen(DbMax.user.position, 'Должность')),
  };

  const createForm = useForm<CreateUserDto>({
    initialValues: {
      name: '',
      surname: '',
      middleName: '',
      email: '',
      phoneNumber: '',
      position: '',
    },
    validate: userFormValidate,
  });

  const editForm = useForm<CreateUserDto>({
    initialValues: {
      name: '',
      surname: '',
      middleName: '',
      email: '',
      phoneNumber: '',
      position: '',
    },
    validate: userFormValidate,
  });

  const createMut = useMutation({
    mutationFn: (dto: CreateUserDto) => createUser(dto),
    onSuccess: () => {
      notifications.show({ title: 'Создано', message: 'Пользователь добавлен', color: 'green' });
      qc.invalidateQueries({ queryKey: ['admin-users'] });
      qc.invalidateQueries({ queryKey: ['admin-user-options'] });
      closeCreate();
      createForm.reset();
    },
    onError: (e: Error) => notifications.show({ title: 'Ошибка', message: e.message, color: 'red' }),
  });

  const updateMut = useMutation({
    mutationFn: ({ id, dto }: { id: number; dto: CreateUserDto }) => updateUser(id, dto),
    onSuccess: () => {
      notifications.show({ title: 'Сохранено', message: 'Данные обновлены', color: 'green' });
      qc.invalidateQueries({ queryKey: ['admin-users'] });
      qc.invalidateQueries({ queryKey: ['admin-user-options'] });
      setEditUser(null);
    },
    onError: (e: Error) => notifications.show({ title: 'Ошибка', message: e.message, color: 'red' }),
  });

  const deleteMut = useMutation({
    mutationFn: (id: number) => deleteUser(id),
    onSuccess: () => {
      notifications.show({ title: 'Удалено', message: 'Пользователь архивирован', color: 'green' });
      qc.invalidateQueries({ queryKey: ['admin-users'] });
      qc.invalidateQueries({ queryKey: ['admin-user-options'] });
      setDeleteUserObj(null);
    },
    onError: (e: Error) => notifications.show({ title: 'Ошибка', message: e.message, color: 'red' }),
  });

  const openEdit = (u: AppUser) => {
    setEditUser(u);
    editForm.setValues({
      name: u.name,
      surname: u.surname,
      middleName: u.middleName ?? '',
      email: u.email,
      phoneNumber: u.phoneNumber,
      position: u.position,
    });
  };

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  return (
    <Stack gap="md">
      <Group justify="space-between" wrap="wrap">
        <Text size="sm" c="dimmed" maw={480}>
          Карточки людей (ФИО, контакты). К логину входа привязывается на вкладке «Аккаунты».
        </Text>
        <Group gap="xs">
          {data != null && (
            <Badge variant="light" color="indigo">
              Всего: {data.totalCount.toLocaleString('ru-RU')}
            </Badge>
          )}
          <Button leftSection={<IconPlus size={16} />} onClick={openCreate} color="indigo">
            Новый пользователь
          </Button>
        </Group>
      </Group>

      {error && (
        <Text c="red" size="sm">
          {(error as Error).message}
        </Text>
      )}

      <CollapsibleFilters activeCount={activeUserFiltersCount}>
        <Paper withBorder radius="md" p="sm">
          <Group gap="sm" wrap="wrap" align="flex-end">
            <TextInput
              size="xs"
              label="Фамилия"
              placeholder="Часть фамилии"
              value={pendingUserFilters.surname}
              onChange={(e) => setPendingUserFilters((p) => ({ ...p, surname: e.target.value }))}
              onKeyDown={onUserFilterKey}
              style={{ flex: 1, minWidth: 120 }}
            />
            <TextInput
              size="xs"
              label="Имя"
              placeholder="Часть имени"
              value={pendingUserFilters.name}
              onChange={(e) => setPendingUserFilters((p) => ({ ...p, name: e.target.value }))}
              onKeyDown={onUserFilterKey}
              style={{ flex: 1, minWidth: 120 }}
            />
            <TextInput
              size="xs"
              label="Отчество"
              placeholder="Часть отчества"
              value={pendingUserFilters.middleName}
              onChange={(e) => setPendingUserFilters((p) => ({ ...p, middleName: e.target.value }))}
              onKeyDown={onUserFilterKey}
              style={{ flex: 1, minWidth: 120 }}
            />
            <TextInput
              size="xs"
              label="Email"
              placeholder="Фрагмент email"
              value={pendingUserFilters.email}
              onChange={(e) => setPendingUserFilters((p) => ({ ...p, email: e.target.value }))}
              onKeyDown={onUserFilterKey}
              style={{ flex: 1, minWidth: 160 }}
            />
            <TextInput
              size="xs"
              label="Телефон"
              placeholder="Фрагмент номера"
              value={pendingUserFilters.phone}
              onChange={(e) => setPendingUserFilters((p) => ({ ...p, phone: e.target.value }))}
              onKeyDown={onUserFilterKey}
              style={{ flex: 1, minWidth: 120 }}
            />
            <TextInput
              size="xs"
              label="Должность"
              placeholder="Часть текста"
              value={pendingUserFilters.position}
              onChange={(e) => setPendingUserFilters((p) => ({ ...p, position: e.target.value }))}
              onKeyDown={onUserFilterKey}
              style={{ flex: 1, minWidth: 140 }}
            />
            <Group gap="xs">
              <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyUserFilters}>
                Найти
              </Button>
              {hasUserFilters && (
                <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetUserFilters}>
                  Сбросить
                </Button>
              )}
            </Group>
          </Group>
        </Paper>
      </CollapsibleFilters>

      <Paper withBorder radius="md" style={{ overflow: 'hidden' }}>
        <ScrollArea>
          <Table striped highlightOnHover verticalSpacing="sm">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>ФИО</Table.Th>
                <Table.Th>Email</Table.Th>
                <Table.Th>Телефон</Table.Th>
                <Table.Th>Должность</Table.Th>
                <Table.Th>Аккаунт</Table.Th>
                <Table.Th w={100} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {isLoading ? (
                <Table.Tr>
                  <Table.Td colSpan={6}>
                    <Text size="sm">Загрузка…</Text>
                  </Table.Td>
                </Table.Tr>
              ) : !items.length ? (
                <Table.Tr>
                  <Table.Td colSpan={6}>
                    <Text size="sm" c="dimmed" ta="center" py="md">
                      {hasUserFilters ? 'Ничего не найдено по фильтрам' : 'Нет пользователей'}
                    </Text>
                  </Table.Td>
                </Table.Tr>
              ) : (
                items.map((u) => (
                  <Table.Tr key={u.id}>
                    <Table.Td>
                      {u.surname} {u.name} {u.middleName || ''}
                    </Table.Td>
                    <Table.Td>{u.email}</Table.Td>
                    <Table.Td>{u.phoneNumber}</Table.Td>
                    <Table.Td>{u.position}</Table.Td>
                    <Table.Td>
                      {u.account ? (
                        <Badge variant="light" color="gray">
                          {u.account.login}
                        </Badge>
                      ) : (
                        <Text size="sm" c="dimmed">
                          —
                        </Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Group gap={4} justify="flex-end">
                        <Tooltip label="Изменить">
                          <ActionIcon variant="subtle" color="indigo" onClick={() => openEdit(u)}>
                            <IconPencil size={16} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Удалить (архив)">
                          <ActionIcon variant="subtle" color="red" onClick={() => setDeleteUserObj(u)}>
                            <IconTrash size={16} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))
              )}
            </Table.Tbody>
          </Table>
        </ScrollArea>
      </Paper>

      <Group justify="space-between">
        <PageSizeSelect value={pageSize} onChange={(v) => { setPageSize(v); setPage(1); }} />
        <Pagination total={totalPages} value={page} onChange={setPage} size="sm" />
      </Group>

      <Modal opened={createOpened} onClose={closeCreate} title="Новый пользователь" centered>
        <form
          onSubmit={createForm.onSubmit((v) =>
            createMut.mutate({
              ...v,
              middleName: v.middleName?.trim() || null,
            })
          )}
        >
          <Stack gap="sm">
            <TextInput label="Имя" required {...createForm.getInputProps('name')} />
            <TextInput label="Фамилия" required {...createForm.getInputProps('surname')} />
            <TextInput label="Отчество" {...createForm.getInputProps('middleName')} />
            <TextInput
              label={
                <Group gap={4} align="center">
                  <span>Email</span>
                  <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openEmailHint} title="Справка по формату email">
                    <IconInfoCircle size={14} />
                  </ActionIcon>
                </Group>
              }
              required
              type="email"
              {...createForm.getInputProps('email')}
            />
            <TextInput
              label={
                <Group gap={4} align="center">
                  <span>Телефон</span>
                  <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openPhoneHint} title="Справка по формату телефона">
                    <IconInfoCircle size={14} />
                  </ActionIcon>
                </Group>
              }
              required
              {...createForm.getInputProps('phoneNumber')}
            />
            <TextInput label="Должность" required {...createForm.getInputProps('position')} />
            <Group justify="flex-end">
              <Button variant="default" onClick={closeCreate}>
                Отмена
              </Button>
              <Button type="submit" loading={createMut.isPending} color="indigo">
                Создать
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>

      <Modal opened={!!editUser} onClose={() => setEditUser(null)} title="Редактирование пользователя" centered>
        {editUser && (
          <form
            onSubmit={editForm.onSubmit((v: CreateUserDto) =>
              updateMut.mutate({
                id: editUser.id,
                dto: { ...v, middleName: v.middleName?.trim() || null },
              })
            )}
          >
            <Stack gap="sm">
              <TextInput label="Имя" required {...editForm.getInputProps('name')} />
              <TextInput label="Фамилия" required {...editForm.getInputProps('surname')} />
              <TextInput label="Отчество" {...editForm.getInputProps('middleName')} />
              <TextInput
                label={
                  <Group gap={4} align="center">
                    <span>Email</span>
                    <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openEmailHint} title="Справка по формату email">
                      <IconInfoCircle size={14} />
                    </ActionIcon>
                  </Group>
                }
                required
                type="email"
                {...editForm.getInputProps('email')}
              />
              <TextInput
                label={
                  <Group gap={4} align="center">
                    <span>Телефон</span>
                    <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openPhoneHint} title="Справка по формату телефона">
                      <IconInfoCircle size={14} />
                    </ActionIcon>
                  </Group>
                }
                required
                {...editForm.getInputProps('phoneNumber')}
              />
              <TextInput label="Должность" required {...editForm.getInputProps('position')} />
              <Group justify="flex-end">
                <Button variant="default" onClick={() => setEditUser(null)}>
                  Отмена
                </Button>
                <Button type="submit" loading={updateMut.isPending} color="indigo">
                  Сохранить
                </Button>
              </Group>
            </Stack>
          </form>
        )}
      </Modal>

      <Modal opened={!!deleteUserObj} onClose={() => setDeleteUserObj(null)} title="Удалить пользователя?" centered>
        <Text size="sm" mb="md">
          Пользователь будет архивирован (мягкое удаление). Продолжить?
        </Text>
        <Group justify="flex-end">
          <Button variant="default" onClick={() => setDeleteUserObj(null)}>
            Отмена
          </Button>
          <Button
            color="red"
            loading={deleteMut.isPending}
            onClick={() => deleteUserObj && deleteMut.mutate(deleteUserObj.id)}
          >
            Удалить
          </Button>
        </Group>
      </Modal>

      <EmailRequirementsModal opened={emailHintOpened} onClose={closeEmailHint} />
      <PhoneRequirementsModal opened={phoneHintOpened} onClose={closePhoneHint} />
    </Stack>
  );
}

function AccountsTab() {
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [pendingAccountFilters, setPendingAccountFilters] = useState<AdminAccountFilters>(EMPTY_ACCOUNT_FILTERS);
  const [appliedAccountFilters, setAppliedAccountFilters] = useState<Record<string, string>>({});
  const [appliedUserLink, setAppliedUserLink] = useState<AdminAccountFilters['userLink']>('all');
  const [rightsAccount, setRightsAccount] = useState<{ id: number; login: string } | null>(null);
  const [activityAccount, setActivityAccount] = useState<{ id: number; login: string } | null>(null);
  const [createOpened, { open: openCreate, close: closeCreate }] = useDisclosure(false);
  const [pwHintOpened, { open: openPwHint, close: closePwHint }] = useDisclosure(false);
  const [editAcc, setEditAcc] = useState<AccountRow | null>(null);
  const [deleteAcc, setDeleteAcc] = useState<AccountRow | null>(null);

  const activeAccountFiltersCount =
    Object.keys(appliedAccountFilters).length + (appliedUserLink !== 'all' ? 1 : 0);
  const hasAccountFilters =
    Object.keys(appliedAccountFilters).length > 0 || appliedUserLink !== 'all';

  const applyAccountFilters = () => {
    setAppliedAccountFilters(buildAccountFilters(pendingAccountFilters));
    setAppliedUserLink(pendingAccountFilters.userLink);
    setPage(1);
  };

  const resetAccountFilters = () => {
    setPendingAccountFilters(EMPTY_ACCOUNT_FILTERS);
    setAppliedAccountFilters({});
    setAppliedUserLink('all');
    setPage(1);
  };

  const onAccountFilterKey = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') applyAccountFilters();
  };

  const { data: usersForSelect } = useQuery({
    queryKey: ['admin-user-options'],
    queryFn: () => getUsers({ pageNumber: 1, pageSize: 500 }),
  });

  const userOptions =
    usersForSelect?.items.map((u) => ({
      value: String(u.id),
      label: `${u.surname} ${u.name} · ${u.email}`,
    })) ?? [];

  const { data, isLoading, error } = useQuery({
    queryKey: ['admin-accounts', page, pageSize, appliedAccountFilters, appliedUserLink],
    queryFn: () =>
      getAccounts({
        pageNumber: page,
        pageSize,
        Filters: Object.keys(appliedAccountFilters).length > 0 ? appliedAccountFilters : undefined,
        userLink: appliedUserLink === 'linked' || appliedUserLink === 'unlinked' ? appliedUserLink : undefined,
      }),
  });

  const createForm = useForm({
    initialValues: {
      login: '',
      password: '',
      description: '',
      linkedUserId: '__none' as string | null,
    },
    validate: {
      login: combine(required('Укажите логин'), maxLen(DbMax.account.login, 'Логин')),
      password: combine(requiredPassword('Пароль'), maxLen(DbMax.account.password, 'Пароль')),
      description: optMaxLen(DbMax.account.description, 'Описание'),
    },
  });

  const editForm = useForm({
    initialValues: {
      login: '',
      password: '',
      description: '',
      linkedUserId: null as string | null,
    },
    validate: {
      login: combine(required('Укажите логин'), maxLen(DbMax.account.login, 'Логин')),
      password: (v) => optionalPassword('Пароль')(v),
      description: optMaxLen(DbMax.account.description, 'Описание'),
    },
  });

  const createMut = useMutation({
    mutationFn: (dto: CreateAccountDto) => createAccount(dto),
    onSuccess: () => {
      notifications.show({ title: 'Создано', message: 'Аккаунт добавлен', color: 'green' });
      qc.invalidateQueries({ queryKey: ['admin-accounts'] });
      closeCreate();
      createForm.reset();
    },
    onError: (e: Error) => notifications.show({ title: 'Ошибка', message: e.message, color: 'red' }),
  });

  const updateMut = useMutation({
    mutationFn: ({
      id,
      dto,
    }: {
      id: number;
      dto: Parameters<typeof updateAccount>[1];
    }) => updateAccount(id, dto),
    onSuccess: () => {
      notifications.show({ title: 'Сохранено', message: 'Аккаунт обновлён', color: 'green' });
      qc.invalidateQueries({ queryKey: ['admin-accounts'] });
      qc.invalidateQueries({ queryKey: ['admin-users'] });
      setEditAcc(null);
    },
    onError: (e: Error) => notifications.show({ title: 'Ошибка', message: e.message, color: 'red' }),
  });

  const deleteMut = useMutation({
    mutationFn: (id: number) => deleteAccount(id),
    onSuccess: () => {
      notifications.show({ title: 'Удалено', message: 'Аккаунт архивирован', color: 'green' });
      qc.invalidateQueries({ queryKey: ['admin-accounts'] });
      setDeleteAcc(null);
    },
    onError: (e: Error) => notifications.show({ title: 'Ошибка', message: e.message, color: 'red' }),
  });

  const openEdit = (a: AccountRow) => {
    setEditAcc(a);
    editForm.setValues({
      login: a.login,
      password: '',
      description: a.description ?? '',
      linkedUserId: a.userId != null ? String(a.userId) : '__none',
    });
  };

  const userSelectData = [
    { value: '__none', label: '— не привязан —' },
    ...userOptions.map((o) => ({ value: o.value, label: o.label })),
  ];

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  return (
    <Stack gap="md">
      <Group justify="space-between" wrap="wrap">
        <Text size="sm" c="dimmed" maw={480}>
          Логин и пароль для входа. Пользователь (карточка) опционален и выбирается из списка.
        </Text>
        <Group gap="xs">
          {data != null && (
            <Badge variant="light" color="violet">
              Всего: {data.totalCount.toLocaleString('ru-RU')}
            </Badge>
          )}
          <Button leftSection={<IconPlus size={16} />} onClick={openCreate} color="indigo">
            Новый аккаунт
          </Button>
        </Group>
      </Group>

      {error && (
        <Text c="red" size="sm">
          {(error as Error).message}
        </Text>
      )}

      <CollapsibleFilters activeCount={activeAccountFiltersCount}>
        <Paper withBorder radius="md" p="sm">
          <Group gap="sm" wrap="wrap" align="flex-end">
            <TextInput
              size="xs"
              label="Логин"
              placeholder="Часть логина"
              value={pendingAccountFilters.login}
              onChange={(e) => setPendingAccountFilters((p) => ({ ...p, login: e.target.value }))}
              onKeyDown={onAccountFilterKey}
              style={{ flex: 1, minWidth: 140 }}
            />
            <TextInput
              size="xs"
              label="Описание"
              placeholder="Часть текста"
              value={pendingAccountFilters.description}
              onChange={(e) => setPendingAccountFilters((p) => ({ ...p, description: e.target.value }))}
              onKeyDown={onAccountFilterKey}
              style={{ flex: 1, minWidth: 160 }}
            />
            <Select
              size="xs"
              label="Привязка к пользователю"
              placeholder="Все"
              clearable={false}
              data={[
                { value: 'all', label: 'Все' },
                { value: 'linked', label: 'С привязкой к карточке' },
                { value: 'unlinked', label: 'Без карточки' },
              ]}
              value={pendingAccountFilters.userLink}
              onChange={(v) =>
                setPendingAccountFilters((p) => ({
                  ...p,
                  userLink: (v as AdminAccountFilters['userLink']) ?? 'all',
                }))
              }
              style={{ flex: '0 0 240px' }}
            />
            <Group gap="xs">
              <Button size="xs" leftSection={<IconSearch size={12} />} onClick={applyAccountFilters}>
                Найти
              </Button>
              {hasAccountFilters && (
                <Button size="xs" variant="subtle" color="gray" leftSection={<IconX size={12} />} onClick={resetAccountFilters}>
                  Сбросить
                </Button>
              )}
            </Group>
          </Group>
        </Paper>
      </CollapsibleFilters>

      <Paper withBorder radius="md" style={{ overflow: 'hidden' }}>
        <ScrollArea>
          <Table striped highlightOnHover verticalSpacing="sm">
            <Table.Thead>
              <Table.Tr>
                <Table.Th>Логин</Table.Th>
                <Table.Th>Пользователь</Table.Th>
                <Table.Th>Описание</Table.Th>
                <Table.Th>Создан</Table.Th>
                <Table.Th w={148} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {isLoading ? (
                <Table.Tr>
                  <Table.Td colSpan={5}>
                    <Text size="sm">Загрузка…</Text>
                  </Table.Td>
                </Table.Tr>
              ) : !items.length ? (
                <Table.Tr>
                  <Table.Td colSpan={5}>
                    <Text size="sm" c="dimmed" ta="center" py="md">
                      {hasAccountFilters ? 'Ничего не найдено по фильтрам' : 'Нет аккаунтов'}
                    </Text>
                  </Table.Td>
                </Table.Tr>
              ) : (
                items.map((a) => (
                  <Table.Tr key={a.id}>
                    <Table.Td>
                      <Text fw={600}>{a.login}</Text>
                    </Table.Td>
                    <Table.Td>
                      {a.user ? (
                        <Text size="sm">
                          {a.user.surname} {a.user.name}
                          <Text span c="dimmed" size="xs" display="block">
                            {a.user.email}
                          </Text>
                        </Text>
                      ) : (
                        <Text size="sm" c="dimmed">
                          не привязан
                        </Text>
                      )}
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" lineClamp={2}>
                        {a.description || '—'}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <Text size="xs">{fmtDateTime(a.createTime)}</Text>
                    </Table.Td>
                    <Table.Td>
                      <Group gap={4} justify="flex-end">
                        <Tooltip label="Активность в системе">
                          <ActionIcon
                            variant="subtle"
                            color="cyan"
                            onClick={() => setActivityAccount({ id: a.id, login: a.login })}
                          >
                            <IconHistory size={16} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Права">
                          <ActionIcon
                            variant="subtle"
                            color="indigo"
                            onClick={() => setRightsAccount({ id: a.id, login: a.login })}
                          >
                            <IconShield size={16} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Изменить">
                          <ActionIcon variant="subtle" color="gray" onClick={() => openEdit(a)}>
                            <IconPencil size={16} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Удалить">
                          <ActionIcon variant="subtle" color="red" onClick={() => setDeleteAcc(a)}>
                            <IconTrash size={16} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))
              )}
            </Table.Tbody>
          </Table>
        </ScrollArea>
      </Paper>

      <Group justify="space-between">
        <PageSizeSelect value={pageSize} onChange={(v) => { setPageSize(v); setPage(1); }} />
        <Pagination total={totalPages} value={page} onChange={setPage} size="sm" />
      </Group>

      <Modal opened={createOpened} onClose={closeCreate} title="Новый аккаунт" centered>
        <form
          onSubmit={createForm.onSubmit((v) => {
            const uid =
              v.linkedUserId && v.linkedUserId !== '__none' ? Number(v.linkedUserId) : null;
            const dto: CreateAccountDto = {
              login: v.login.trim(),
              password: v.password,
              description: v.description?.trim() || null,
              userId: uid != null && !Number.isNaN(uid) ? uid : null,
            };
            createMut.mutate(dto);
          })}
        >
          <Stack gap="sm">
            <TextInput label="Логин" required {...createForm.getInputProps('login')} />
            <TextInput
              label={
                <Group gap={4} align="center">
                  <span>Пароль</span>
                  <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openPwHint} title="Справка по паролю">
                    <IconInfoCircle size={14} />
                  </ActionIcon>
                </Group>
              }
              type="password"
              required
              autoComplete="new-password"
              {...createForm.getInputProps('password')}
            />
            <TextInput label="Описание" {...createForm.getInputProps('description')} />
            <Select
              label="Пользователь (карточка)"
              description="Можно привязать позже"
              data={userSelectData}
              clearable
              {...createForm.getInputProps('linkedUserId')}
            />
            <Group justify="flex-end">
              <Button variant="default" onClick={closeCreate}>
                Отмена
              </Button>
              <Button type="submit" loading={createMut.isPending} color="indigo">
                Создать
              </Button>
            </Group>
          </Stack>
        </form>
      </Modal>

      <Modal opened={!!editAcc} onClose={() => setEditAcc(null)} title="Редактирование аккаунта" centered>
        {editAcc && (
          <form
            onSubmit={editForm.onSubmit((v) => {
              const dto: UpdateAccountDto = {
                login: v.login.trim(),
                description: v.description?.trim() || null,
              };
              if (v.password.trim()) dto.password = v.password.trim();
              if (!v.linkedUserId || v.linkedUserId === '__none') dto.unlinkUser = true;
              else dto.userId = Number(v.linkedUserId);
              updateMut.mutate({ id: editAcc.id, dto });
            })}
          >
            <Stack gap="sm">
              <TextInput label="Логин" required {...editForm.getInputProps('login')} />
              <TextInput
                label={
                  <Group gap={4} align="center">
                    <span>Новый пароль</span>
                    <ActionIcon size="xs" variant="transparent" color="dimmed" onClick={openPwHint} title="Справка по паролю">
                      <IconInfoCircle size={14} />
                    </ActionIcon>
                  </Group>
                }
                description="Оставьте пустым, чтобы не менять"
                type="password"
                autoComplete="new-password"
                {...editForm.getInputProps('password')}
              />
              <TextInput label="Описание" {...editForm.getInputProps('description')} />
              <Select
                label="Пользователь (карточка)"
                data={userSelectData}
                searchable
                {...editForm.getInputProps('linkedUserId')}
              />
              <Group justify="flex-end">
                <Button variant="default" onClick={() => setEditAcc(null)}>
                  Отмена
                </Button>
                <Button type="submit" loading={updateMut.isPending} color="indigo">
                  Сохранить
                </Button>
              </Group>
            </Stack>
          </form>
        )}
      </Modal>

      <Modal opened={!!deleteAcc} onClose={() => setDeleteAcc(null)} title="Удалить аккаунт?" centered>
        <Text size="sm" mb="md">
          Аккаунт «{deleteAcc?.login}» будет архивирован. Продолжить?
        </Text>
        <Group justify="flex-end">
          <Button variant="default" onClick={() => setDeleteAcc(null)}>
            Отмена
          </Button>
          <Button
            color="red"
            loading={deleteMut.isPending}
            onClick={() => deleteAcc && deleteMut.mutate(deleteAcc.id)}
          >
            Удалить
          </Button>
        </Group>
      </Modal>

      <AdminAccountRightsModal
        opened={rightsAccount != null}
        onClose={() => setRightsAccount(null)}
        accountId={rightsAccount?.id ?? null}
        loginLabel={rightsAccount?.login ?? ''}
      />

      <AdminAccountActivityDrawer
        opened={activityAccount != null}
        onClose={() => setActivityAccount(null)}
        accountId={activityAccount?.id ?? null}
        loginLabel={activityAccount?.login ?? ''}
      />

      <PasswordRequirementsModal opened={pwHintOpened} onClose={closePwHint} />
    </Stack>
  );
}

export function AdminPage() {
  return (
    <Stack gap="lg">
      <div>
        <Title order={2} c="indigo">
          Администрирование
        </Title>
        <Text c="dimmed" mt={4} maw={720}>
          Управление пользователями (профили), аккаунтами входа и правами. Готовые роли: полный доступ, только админка,
          оператор, сотрудник бэк-офиса.
        </Text>
      </div>

      <AdminMetricsStrip />

      <Tabs defaultValue="users" variant="outline" radius="md">
        <Tabs.List>
          <Tabs.Tab value="users" leftSection={<IconUsers size={16} />}>
            Пользователи
          </Tabs.Tab>
          <Tabs.Tab value="accounts" leftSection={<IconKey size={16} />}>
            Аккаунты
          </Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="users" pt="lg">
          <UsersTab />
        </Tabs.Panel>
        <Tabs.Panel value="accounts" pt="lg">
          <AccountsTab />
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}
