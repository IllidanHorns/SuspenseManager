import { Box, Group, List, Modal, Paper, Text, ThemeIcon } from '@mantine/core';
import { IconBuildingBank, IconId, IconInfoCircle } from '@tabler/icons-react';

type Props = { opened: boolean; onClose: () => void };

export function CompanyIdentifiersModal({ opened, onClose }: Props) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      size="lg"
      centered
      title={
        <Group gap="sm" wrap="nowrap">
          <ThemeIcon size={36} radius="md" color="violet" variant="light">
            <IconInfoCircle size={20} />
          </ThemeIcon>
          <Box>
            <Text fw={700} size="lg">Реквизиты компании: ИНН и БИК</Text>
            <Text size="xs" c="dimmed" mt={2}>Требования к формату банковских и налоговых идентификаторов</Text>
          </Box>
        </Group>
      }
    >
      <Paper withBorder radius="md" p="md" mb="md">
        <Group gap="sm" align="flex-start" wrap="nowrap" mb="sm">
          <ThemeIcon size={36} radius="md" color="orange" variant="light">
            <IconId size={18} />
          </ThemeIcon>
          <Box>
            <Text fw={700} size="sm">ИНН — Идентификационный Номер Налогоплательщика</Text>
            <Text size="xs" c="dimmed">Присваивается налоговой инспекцией</Text>
          </Box>
        </Group>

        <List spacing="xs" size="sm" c="dimmed" icon={<Text c="orange" size="sm">•</Text>}>
          <List.Item>
            <Text span inherit c="dark">
              Только цифры, без букв и пробелов.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              <Text span fw={600} c="dark">10 цифр</Text>{' '}— для юридических лиц и ИП.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              <Text span fw={600} c="dark">12 цифр</Text>{' '}— для физических лиц.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Пример ЮЛ:{' '}
              <Text span fw={600} c="dark" ff="monospace">7707083893</Text>
              {' '}(Сбербанк).
            </Text>
          </List.Item>
        </List>
      </Paper>

      <Paper withBorder radius="md" p="md">
        <Group gap="sm" align="flex-start" wrap="nowrap" mb="sm">
          <ThemeIcon size={36} radius="md" color="violet" variant="light">
            <IconBuildingBank size={18} />
          </ThemeIcon>
          <Box>
            <Text fw={700} size="sm">БИК — Банковский Идентификационный Код</Text>
            <Text size="xs" c="dimmed">Присваивается Центральным Банком России</Text>
          </Box>
        </Group>

        <List spacing="xs" size="sm" c="dimmed" icon={<Text c="violet" size="sm">•</Text>}>
          <List.Item>
            <Text span inherit c="dark">
              Ровно <Text span fw={600} c="dark">9 цифр</Text>.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Первые два символа — код страны (для России:{' '}
              <Text span fw={600} c="dark" ff="monospace">04</Text>).
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Пример:{' '}
              <Text span fw={600} c="dark" ff="monospace">044525225</Text>
              {' '}(Сбербанк Москва).
            </Text>
          </List.Item>
        </List>
      </Paper>
    </Modal>
  );
}
