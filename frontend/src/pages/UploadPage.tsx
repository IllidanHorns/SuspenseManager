import { useState, useRef } from 'react';
import {
  Stack,
  Title,
  Text,
  Paper,
  Button,
  Group,
  Alert,
  SimpleGrid,
  Card,
  Table,
  ScrollArea,
  Badge,
  Box,
  Progress,
  ThemeIcon,
} from '@mantine/core';
import {
  IconUpload,
  IconFile,
  IconAlertCircle,
  IconCircleCheck,
  IconAlertTriangle,
  IconX,
} from '@tabler/icons-react';
import { notifications } from '@mantine/notifications';
import { uploadFile } from '../api/upload';
import { STATUS_LABELS, STATUS_COLORS } from '../types';
import type { ValidationResultDto } from '../types';

export function UploadPage() {
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<ValidationResultDto | null>(null);
  const [error, setError] = useState('');
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0];
    if (!f) return;
    if (!f.name.endsWith('.xlsx') && !f.name.endsWith('.xls')) {
      setError('Разрешены только файлы .xlsx и .xls');
      return;
    }
    setError('');
    setFile(f);
    setResult(null);
  };

  const handleUpload = async () => {
    if (!file) return;
    setLoading(true);
    setError('');
    try {
      const res = await uploadFile(file);
      setResult(res);
      notifications.show({
        title: 'Загрузка завершена',
        message: `Обработано ${res.totalRows} строк`,
        color: 'green',
        icon: <IconCircleCheck size={16} />,
      });
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : 'Ошибка загрузки');
    } finally {
      setLoading(false);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    const f = e.dataTransfer.files[0];
    if (f && (f.name.endsWith('.xlsx') || f.name.endsWith('.xls'))) {
      setFile(f);
      setResult(null);
      setError('');
    }
  };

  return (
    <Stack gap="xl">
      <Box>
        <Title order={3} fw={600}>Загрузка отчёта</Title>
        <Text c="dimmed" size="sm">Загрузите Excel-файл со стриминговым отчётом для валидации</Text>
      </Box>

      <Paper
        withBorder
        radius="md"
        p="xl"
        style={{
          borderStyle: 'dashed',
          borderWidth: 2,
          textAlign: 'center',
          cursor: 'pointer',
        }}
        onDragOver={(e) => e.preventDefault()}
        onDrop={handleDrop}
        onClick={() => inputRef.current?.click()}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx,.xls"
          style={{ display: 'none' }}
          onChange={handleFileChange}
        />
        <ThemeIcon size={56} radius="xl" color="indigo" variant="light" mx="auto" mb="md">
          <IconUpload size={28} />
        </ThemeIcon>
        <Text size="lg" fw={500} mb={4}>
          {file ? file.name : 'Перетащите файл или нажмите для выбора'}
        </Text>
        <Text size="sm" c="dimmed">Поддерживаются форматы .xlsx и .xls (до 50 МБ)</Text>

        {file && (
          <Group justify="center" mt="md" gap="xs">
            <IconFile size={16} />
            <Text size="sm" fw={500}>{file.name}</Text>
            <Text size="sm" c="dimmed">({(file.size / 1024).toFixed(0)} KB)</Text>
          </Group>
        )}
      </Paper>

      {error && (
        <Alert icon={<IconAlertCircle size={16} />} color="red" radius="md">
          {error}
        </Alert>
      )}

      {file && (
        <Group>
          <Button
            leftSection={<IconUpload size={16} />}
            color="indigo"
            loading={loading}
            onClick={handleUpload}
          >
            Загрузить и валидировать
          </Button>
          <Button
            variant="subtle"
            color="gray"
            leftSection={<IconX size={16} />}
            onClick={() => { setFile(null); setResult(null); setError(''); }}
          >
            Очистить
          </Button>
        </Group>
      )}

      {result && (
        <Stack gap="md">
          <SimpleGrid cols={{ base: 2, sm: 4 }} spacing="md">
            {[
              { label: 'Всего строк', value: result.totalRows, color: 'blue', icon: IconFile },
              { label: 'Прошло валидацию', value: result.validatedCount, color: 'green', icon: IconCircleCheck },
              { label: 'Нет продукта', value: result.noProductCount, color: 'orange', icon: IconAlertTriangle },
              { label: 'Нет прав', value: result.noRightsCount, color: 'red', icon: IconAlertCircle },
            ].map(({ label, value, color, icon: Icon }) => (
              <Card withBorder radius="md" p="md" key={label}>
                <Group justify="space-between" mb={4}>
                  <Text size="xs" c="dimmed" fw={600} tt="uppercase">{label}</Text>
                  <ThemeIcon size={28} radius="md" color={color} variant="light">
                    <Icon size={14} />
                  </ThemeIcon>
                </Group>
                <Text size="xl" fw={700}>{value}</Text>
                <Progress
                  value={result.totalRows > 0 ? (value / result.totalRows) * 100 : 0}
                  color={color}
                  size="xs"
                  mt="sm"
                  radius="xl"
                />
              </Card>
            ))}
          </SimpleGrid>

          <Paper withBorder radius="md">
            <Box p="md" style={{ borderBottom: '1px solid var(--mantine-color-default-border)' }}>
              <Text fw={600}>Результаты валидации ({result.lines.length} строк)</Text>
            </Box>
            <ScrollArea h={400}>
              <Table striped highlightOnHover>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>ID строки</Table.Th>
                    <Table.Th>Статус</Table.Th>
                    <Table.Th>Причина</Table.Th>
                    <Table.Th>ID продукта</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {result.lines.map((line) => (
                    <Table.Tr key={line.suspenseLineId}>
                      <Table.Td>{line.suspenseLineId}</Table.Td>
                      <Table.Td>
                        <Badge color={STATUS_COLORS[line.businessStatus] ?? 'gray'} variant="light" size="sm">
                          {STATUS_LABELS[line.businessStatus] ?? line.businessStatus}
                        </Badge>
                      </Table.Td>
                      <Table.Td>
                        <Text size="sm" c="dimmed">{line.causeSuspense ?? '—'}</Text>
                      </Table.Td>
                      <Table.Td>{line.productId ?? '—'}</Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </ScrollArea>
          </Paper>
        </Stack>
      )}
    </Stack>
  );
}
