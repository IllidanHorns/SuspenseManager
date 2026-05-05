import { Box, Group, List, Modal, Paper, Text, ThemeIcon } from '@mantine/core';
import { IconInfoCircle, IconMail } from '@tabler/icons-react';

type Props = { opened: boolean; onClose: () => void };

export function EmailRequirementsModal({ opened, onClose }: Props) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      size="md"
      centered
      title={
        <Group gap="sm" wrap="nowrap">
          <ThemeIcon size={36} radius="md" color="teal" variant="light">
            <IconInfoCircle size={20} />
          </ThemeIcon>
          <Box>
            <Text fw={700} size="lg">Справка по формату email</Text>
            <Text size="xs" c="dimmed" mt={2}>Требования к адресу электронной почты</Text>
          </Box>
        </Group>
      }
    >
      <Paper withBorder radius="md" p="md">
        <Group gap="sm" align="flex-start" wrap="nowrap" mb="sm">
          <ThemeIcon size={36} radius="md" color="teal" variant="light">
            <IconMail size={18} />
          </ThemeIcon>
          <Text fw={600} size="sm" style={{ lineHeight: 1.35 }}>
            Адрес электронной почты используется для уведомлений и идентификации пользователя в системе.
          </Text>
        </Group>

        <List spacing="xs" size="sm" c="dimmed" icon={<Text c="teal" size="sm">•</Text>}>
          <List.Item>
            <Text span inherit c="dark">
              Обязательно наличие символа{' '}
              <Text span fw={600} c="dark" ff="monospace">@</Text>.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              До и после <Text span fw={600} c="dark" ff="monospace">@</Text> должны быть символы без пробелов.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Максимальная длина:{' '}
              <Text span fw={600} c="dark">255 символов</Text>.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Пример:{' '}
              <Text span fw={600} c="dark" ff="monospace">user@company.ru</Text>
              {' '}или{' '}
              <Text span fw={600} c="dark" ff="monospace">ivan.petrov@music.org</Text>.
            </Text>
          </List.Item>
        </List>
      </Paper>
    </Modal>
  );
}
