import * as pdfMakeNs from 'pdfmake/build/pdfmake';
import pdfFonts from 'pdfmake/build/vfs_fonts';
import type { Content, TDocumentDefinitions } from 'pdfmake/interfaces';
import { STATUS_LABELS } from '../types';

/** Vite ESM: singleton на `default`, иначе на корне namespace */
const pdfMake = (pdfMakeNs as { default?: typeof pdfMakeNs }).default ?? pdfMakeNs;

pdfMake.addVirtualFileSystem(pdfFonts);

export interface DashboardPdfData {
  totalSuspenses: number;
  noProductCount: number;
  noRightsCount: number;
  inGroupNoProduct: number;
  inGroupNoRights: number;
  validatedCount: number;
  backOfficeCount: number;
  postponedCount: number;
  totalGroups: number;
  totalProducts: number;
  totalCompanies: number;
  totalRevenue: number;
  totalStreams: number;
  topOperators: { operator: string; count: number; revenue: number }[];
  statusDistribution: { status: number; count: number }[];
}

/** Data URL (png) снимков блоков дашборда с экрана */
export interface DashboardPdfScreenCaptures {
  statCards?: string;
  summary?: string;
  donut?: string;
  bar?: string;
}

const STAT_ROWS: { key: keyof DashboardPdfData; label: string }[] = [
  { key: 'noProductCount', label: 'Нет продукта' },
  { key: 'noRightsCount', label: 'Нет прав' },
  { key: 'inGroupNoProduct', label: 'В группах (нет продукта)' },
  { key: 'inGroupNoRights', label: 'В группах (нет прав)' },
  { key: 'postponedCount', label: 'Отложено' },
  { key: 'backOfficeCount', label: 'Бэк-офис' },
  { key: 'validatedCount', label: 'Прошло валидацию' },
  { key: 'totalGroups', label: 'Групп всего' },
];

const m = (top: number, right: number, bottom: number, left: number): [number, number, number, number] => [
  top,
  right,
  bottom,
  left,
];

const nf = (n: number) => n.toLocaleString('ru-RU');
const money = (n: number) =>
  n.toLocaleString('ru-RU', { maximumFractionDigits: 2, minimumFractionDigits: 0 });

const IMG_WIDTH = 500;

function pushScreenshot(blocks: Content[], title: string, dataUrl: string | undefined): void {
  if (!dataUrl) return;
  blocks.push(
    { text: title, fontSize: 12, bold: true, margin: m(0, 0, 0, 6) },
    {
      image: dataUrl,
      width: IMG_WIDTH,
      alignment: 'center',
      margin: m(0, 0, 0, 14),
    }
  );
}

function buildContent(data: DashboardPdfData, captures?: DashboardPdfScreenCaptures): Content {
  const generated = new Date().toLocaleString('ru-RU', {
    dateStyle: 'long',
    timeStyle: 'short',
  });

  const statTableBody = [
    [
      { text: 'Показатель', bold: true, fillColor: '#eeeeee' },
      { text: 'Значение', bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
    ],
    ...STAT_ROWS.map(({ key, label }) => [
      { text: label },
      { text: nf(data[key] as number), alignment: 'right' as const },
    ]),
  ];

  const statusBody = [
    [
      { text: 'Код', bold: true, fillColor: '#eeeeee' },
      { text: 'Статус', bold: true, fillColor: '#eeeeee' },
      { text: 'Кол-во', bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
    ],
    ...[...data.statusDistribution]
      .sort((a, b) => a.status - b.status)
      .map((s) => [
        { text: String(s.status) },
        { text: STATUS_LABELS[s.status] ?? `Статус ${s.status}` },
        { text: nf(s.count), alignment: 'right' as const },
      ]),
  ];

  const opsBody = [
    [
      { text: 'Оператор', bold: true, fillColor: '#eeeeee' },
      { text: 'Суспенсов', bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
      { text: 'Выручка', bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
    ],
    ...data.topOperators.map((o) => [
      { text: o.operator || '—' },
      { text: nf(o.count), alignment: 'right' as const },
      { text: money(o.revenue), alignment: 'right' as const },
    ]),
  ];

  const hasCharts =
    !!captures?.statCards ||
    !!captures?.summary ||
    !!captures?.donut ||
    !!captures?.bar;

  const blocks: Content[] = [
    { text: 'Дашборд SuspenseManager', fontSize: 18, bold: true, margin: m(0, 0, 0, 8) },
    { text: `Сформировано: ${generated}`, fontSize: 9, color: '#666666', margin: m(0, 0, 0, 12) },
  ];

  if (hasCharts) {
    blocks.push({
      text: 'Снимки экрана (как на дашборде). Таблицы ниже дублируют данные там, где снимок не сделан.',
      fontSize: 9,
      color: '#444444',
      italics: true,
      margin: m(0, 0, 0, 12),
    });
    pushScreenshot(blocks, 'Карточки показателей', captures?.statCards);
    pushScreenshot(blocks, 'Сводка: всего суспенсов, стримы, выручка', captures?.summary);
    pushScreenshot(blocks, 'Распределение по статусам', captures?.donut);
    pushScreenshot(blocks, 'Топ операторов по количеству суспенсов', captures?.bar);
  }

  blocks.push({ text: 'Сводные показатели (числа)', fontSize: 13, bold: true, margin: m(0, 0, 0, 8) });
  blocks.push({
    table: {
      widths: ['*', 100],
      body: [
        [
          { text: 'Всего суспенсов' },
          { text: nf(data.totalSuspenses), alignment: 'right' as const },
        ],
        [
          { text: 'Стримов' },
          { text: nf(data.totalStreams), alignment: 'right' as const },
        ],
        [
          { text: 'Выручка (суспенсы)' },
          { text: money(data.totalRevenue), alignment: 'right' as const },
        ],
      ],
    },
    layout: 'lightHorizontalLines',
    margin: m(0, 0, 0, 16),
  });

  if (!captures?.statCards) {
    blocks.push(
      { text: 'Карточки (таблица)', fontSize: 13, bold: true, margin: m(0, 0, 0, 8) },
      {
        table: { widths: ['*', 80], body: statTableBody },
        layout: 'lightHorizontalLines',
        margin: m(0, 0, 0, 16),
      }
    );
  }

  if (!captures?.donut) {
    blocks.push({ text: 'Распределение по статусам', fontSize: 13, bold: true, margin: m(0, 0, 0, 8) });
    if (data.statusDistribution.length === 0) {
      blocks.push({ text: 'Нет данных', italics: true, margin: m(0, 0, 0, 16) });
    } else {
      blocks.push({
        table: { widths: [40, '*', 70], body: statusBody },
        layout: 'lightHorizontalLines',
        margin: m(0, 0, 0, 16),
      });
    }
  }

  if (!captures?.bar) {
    blocks.push(
      { text: 'Топ операторов по количеству суспенсов', fontSize: 13, bold: true, margin: m(0, 0, 0, 8) }
    );
    if (data.topOperators.length === 0) {
      blocks.push({ text: 'Нет данных', italics: true, margin: m(0, 0, 0, 16) });
    } else {
      blocks.push({
        table: { widths: ['*', 70, 90], body: opsBody },
        layout: 'lightHorizontalLines',
        margin: m(0, 0, 0, 16),
      });
    }
  }

  blocks.push(
    { text: 'Справочники', fontSize: 13, bold: true, margin: m(0, 0, 0, 8) },
    {
      table: {
        widths: ['*', 100],
        body: [
          [
            { text: 'Продуктов в каталоге' },
            { text: nf(data.totalProducts), alignment: 'right' as const },
          ],
          [
            { text: 'Компаний' },
            { text: nf(data.totalCompanies), alignment: 'right' as const },
          ],
        ],
      },
      layout: 'lightHorizontalLines',
      margin: m(0, 0, 0, 16),
    },
    { text: 'Разделы приложения (навигация)', fontSize: 13, bold: true, margin: m(0, 0, 0, 8) },
    {
      ul: [
        'Загрузить отчёт — /upload',
        'Создать группировку — /grouping',
        'Сохранённые группы — /groups',
        'Отложенные группы — /postponed',
      ],
      margin: m(0, 0, 0, 8),
    }
  );

  return blocks;
}

export function downloadDashboardPdf(
  data: DashboardPdfData,
  captures?: DashboardPdfScreenCaptures
): Promise<void> {
  const dd: TDocumentDefinitions = {
    content: buildContent(data, captures),
    defaultStyle: { font: 'Roboto', fontSize: 10 },
    pageMargins: [40, 48, 40, 48] as [number, number, number, number],
  };
  const stamp = new Date().toISOString().slice(0, 10);
  return pdfMake.createPdf(dd).download(`suspensemanager-dashboard-${stamp}.pdf`);
}
