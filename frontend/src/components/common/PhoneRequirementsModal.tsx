import { Box, Group, List, Modal, Paper, Text, ThemeIcon } from '@mantine/core';
import { IconInfoCircle, IconPhone } from '@tabler/icons-react';

type Props = { opened: boolean; onClose: () => void };

export function PhoneRequirementsModal({ opened, onClose }: Props) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      size="md"
      centered
      title={
        <Group gap="sm" wrap="nowrap">
          <ThemeIcon size={36} radius="md" color="blue" variant="light">
            <IconInfoCircle size={20} />
          </ThemeIcon>
          <Box>
            <Text fw={700} size="lg">Справка по формату телефона</Text>
            <Text size="xs" c="dimmed" mt={2}>Допустимые форматы номера телефона</Text>
          </Box>
        </Group>
      }
    >
      <Paper withBorder radius="md" p="md">
        <Group gap="sm" align="flex-start" wrap="nowrap" mb="sm">
          <ThemeIcon size={36} radius="md" color="blue" variant="light">
            <IconPhone size={18} />
          </ThemeIcon>
          <Text fw={600} size="sm" style={{ lineHeight: 1.35 }}>
            Укажите контактный номер телефона в российском или международном формате.
          </Text>
        </Group>

        <List spacing="xs" size="sm" c="dimmed" icon={<Text c="blue" size="sm">•</Text>}>
          <List.Item>
            <Text span inherit c="dark">
              Длина: от{' '}
              <Text span fw={600} c="dark">7 до 20</Text> символов.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Допустимы цифры, знак{' '}
              <Text span fw={600} c="dark" ff="monospace">+</Text>
              , пробелы, дефисы и скобки.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Российский мобильный:{' '}
              <Text span fw={600} c="dark" ff="monospace">+7 (900) 000-00-00</Text>
              {' '}или{' '}
              <Text span fw={600} c="dark" ff="monospace">+79000000000</Text>.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Московский городской:{' '}
              <Text span fw={600} c="dark" ff="monospace">+7 (495) 000-00-00</Text>.
            </Text>
          </List.Item>
          <List.Item>
            <Text span inherit c="dark">
              Международный:{' '}
              <Text span fw={600} c="dark" ff="monospace">+1 (800) 555-12-34</Text>.
            </Text>
          </List.Item>
        </List>
      </Paper>
    </Modal>
  );
}
