import { Box, Group, List, Modal, Paper, Text, ThemeIcon } from '@mantine/core';
import { IconInfoCircle, IconKey, IconShieldCheck } from '@tabler/icons-react';
import { PASSWORD_ALLOWED_SPECIALS, PASSWORD_MIN_LENGTH } from '../../utils/passwordPolicy';

type PasswordRequirementsModalProps = {
  opened: boolean;
  onClose: () => void;
};

export function PasswordRequirementsModal({ opened, onClose }: PasswordRequirementsModalProps) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      size="lg"
      centered
      title={
        <Group gap="sm" wrap="nowrap">
          <ThemeIcon size={36} radius="md" color="indigo" variant="light">
            <IconInfoCircle size={20} />
          </ThemeIcon>
          <Box>
            <Text fw={700} size="lg">
              Справка по паролю
            </Text>
            <Text size="xs" c="dimmed" mt={2}>
              Требования к паролю для новых и обновляемых аккаунтов
            </Text>
          </Box>
        </Group>
      }
    >
      <Paper withBorder radius="md" p="md">
        <Group gap="sm" align="flex-start" wrap="nowrap" mb="sm">
          <ThemeIcon size={36} radius="md" color="grape" variant="light">
            <IconKey size={18} />
          </ThemeIcon>
          <Text fw={600} size="sm" style={{ lineHeight: 1.35 }}>
            Пароль должен быть достаточно сложным, чтобы его нельзя было легко подобрать.
          </Text>
        </Group>

        <List spacing="xs" size="sm" c="dimmed" icon={<Text c="grape" size="sm">•</Text>}>
          <List.Item>
            <Text span inherit c="dark">
              Минимум{' '}
              <Text span fw={600} c="dark">
                {PASSWORD_MIN_LENGTH} символов
              </Text>
              .
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Только{' '}
              <Text span fw={600} c="dark">
                латинские буквы
              </Text>
              , цифры и разрешённые спецсимволы.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Обязательно должна быть хотя бы{' '}
              <Text span fw={600} c="dark">
                одна строчная
              </Text>{' '}
              буква,{' '}
              <Text span fw={600} c="dark">
                одна заглавная
              </Text>
              ,{' '}
              <Text span fw={600} c="dark">
                одна цифра
              </Text>{' '}
              и{' '}
              <Text span fw={600} c="dark">
                один спецсимвол
              </Text>
              .
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Разрешённые спецсимволы:{' '}
              <Text span fw={600} c="dark" ff="monospace">
                {PASSWORD_ALLOWED_SPECIALS}
              </Text>
              .
            </Text>
          </List.Item>
        </List>
      </Paper>

      <Paper withBorder radius="md" p="md" mt="md">
        <Group gap="sm" align="flex-start" wrap="nowrap" mb="sm">
          <ThemeIcon size={36} radius="md" color="green" variant="light">
            <IconShieldCheck size={18} />
          </ThemeIcon>
          <Text fw={600} size="sm" style={{ lineHeight: 1.35 }}>
            Пример подходящего пароля
          </Text>
        </Group>
        <Text size="sm" c="dimmed">
          Например:{' '}
          <Text span fw={600} c="dark" ff="monospace">
            Admin2026!
          </Text>{' '}
          или{' '}
          <Text span fw={600} c="dark" ff="monospace">
            Music_Rights8#
          </Text>
          .
        </Text>
      </Paper>
    </Modal>
  );
}
