import {
  Modal, Stack, Group, Text, Badge, Divider,
  Loader, Center, Table, ScrollArea, ThemeIcon,
} from '@mantine/core';
import { IconCircleCheck, IconCircleX } from '@tabler/icons-react';
import { useQuery } from '@tanstack/react-query';
import { getCatalogProduct, getCatalogRights } from '../../api/catalog';
import { fmtDate } from '../../utils/format';
import type { CatalogProduct } from '../../types';

function Field({ label, value }: { label: string; value?: string | number | null }) {
  return (
    <Group gap="xs" align="flex-start">
      <Text size="xs" c="dimmed" w={130} style={{ flexShrink: 0 }}>{label}</Text>
      <Text size="sm">{value ?? '—'}</Text>
    </Group>
  );
}

interface Props {
  productId: number | null;
  onClose: () => void;
}

export function ProductDetailModal({ productId, onClose }: Props) {
  const opened = productId !== null;

  const { data: product, isLoading: loadingProduct } = useQuery({
    queryKey: ['catalog-product-detail', productId],
    queryFn: () => getCatalogProduct(productId!),
    enabled: opened,
  });

  const { data: rightsPage, isLoading: loadingRights } = useQuery({
    queryKey: ['catalog-product-rights', productId],
    queryFn: () => getCatalogRights({ productId: productId!, pageSize: 100 }),
    enabled: opened,
  });

  const rights = rightsPage?.items ?? [];
  const hasRights = rights.length > 0;

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={<Text fw={600}>Карточка продукта{product ? ` #${product.id}` : ''}</Text>}
      size="lg"
      radius="md"
      centered
    >
      {(loadingProduct || loadingRights) ? (
        <Center py="xl"><Loader size="sm" /></Center>
      ) : !product ? (
        <Text c="dimmed" ta="center" py="xl">Продукт не найден</Text>
      ) : (
        <Stack gap="md">
          <ProductFields product={product} />

          <Divider />

          <Group gap="xs">
            <ThemeIcon
              size="sm"
              variant="light"
              color={hasRights ? 'teal' : 'red'}
              radius="xl"
            >
              {hasRights
                ? <IconCircleCheck size={14} />
                : <IconCircleX size={14} />
              }
            </ThemeIcon>
            <Text size="sm" fw={500}>
              {hasRights
                ? `Права есть (${rights.length} запись${rights.length > 1 ? 'ей' : ''})`
                : 'Прав нет'}
            </Text>
          </Group>

          {hasRights && (
            <ScrollArea>
              <Table striped fz="xs" style={{ minWidth: 560 }}>
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th>Отправитель</Table.Th>
                    <Table.Th>Получатель</Table.Th>
                    <Table.Th>Территория</Table.Th>
                    <Table.Th>Договор</Table.Th>
                    <Table.Th>Период</Table.Th>
                    <Table.Th>Доля</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {rights.map((r) => (
                    <Table.Tr key={r.id}>
                      <Table.Td>{r.companySender || '—'}</Table.Td>
                      <Table.Td>{r.companyReceiver || '—'}</Table.Td>
                      <Table.Td>
                        <Badge size="xs" variant="light">{r.territoryCode || '—'}</Badge>
                      </Table.Td>
                      <Table.Td>{r.docNumber || '—'}</Table.Td>
                      <Table.Td style={{ whiteSpace: 'nowrap' }}>
                        {r.docStart ? fmtDate(r.docStart) : '—'}
                        {r.docEnd ? ` — ${fmtDate(r.docEnd)}` : ''}
                      </Table.Td>
                      <Table.Td>{r.share != null ? `${r.share}%` : '—'}</Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </ScrollArea>
          )}
        </Stack>
      )}
    </Modal>
  );
}

function ProductFields({ product }: { product: CatalogProduct }) {
  return (
    <Stack gap={6}>
      <Field label="Название" value={product.productName} />
      <Field label="Артист" value={product.artist} />
      <Field label="ISRC" value={product.isrc} />
      <Field label="Баркод" value={product.barcode} />
      <Field label="Каталожный №" value={product.catalogNumber} />
      <Field label="Жанр" value={product.genre} />
      <Field label="Длительность" value={product.duration} />
      <Field label="Дата выпуска" value={product.releaseDate ? fmtDate(product.releaseDate) : null} />
    </Stack>
  );
}
