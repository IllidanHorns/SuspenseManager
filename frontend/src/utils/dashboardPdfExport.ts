import * as pdfMakeNs from 'pdfmake/build/pdfmake';
import pdfFonts from 'pdfmake/build/vfs_fonts';
import type { Content, TDocumentDefinitions } from 'pdfmake/interfaces';
import { STATUS_LABELS } from '../types';

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
  topTerritories: { territoryCode: string; count: number; revenue: number }[];
  topCompanies: { companyName: string; count: number }[];
}

export interface DashboardPdfScreenCaptures {
  statCards?: string;
  summary?: string;
  donut?: string;
  bar?: string;
  revBar?: string;
  terrBar?: string;
  compBar?: string;
}

export interface DashboardPdfFilterPeriod {
  from?: string;
  to?: string;
}

const STAT_ROWS: { key: keyof DashboardPdfData; label: string }[] = [
  { key: 'noProductCount',   label: 'Нет продукта' },
  { key: 'noRightsCount',    label: 'Нет прав' },
  { key: 'inGroupNoProduct', label: 'В группах (нет продукта)' },
  { key: 'inGroupNoRights',  label: 'В группах (нет прав)' },
  { key: 'postponedCount',   label: 'Отложено' },
  { key: 'backOfficeCount',  label: 'Бэк-офис' },
  { key: 'validatedCount',   label: 'Прошло валидацию' },
  { key: 'totalGroups',      label: 'Групп всего' },
];

const m = (top: number, right: number, bottom: number, left: number): [number, number, number, number] =>
  [top, right, bottom, left];

const nf    = (n: number) => n.toLocaleString('ru-RU');
const money = (n: number) => n.toLocaleString('ru-RU', { maximumFractionDigits: 2, minimumFractionDigits: 0 });

const IMG_WIDTH = 500;

function pushScreenshot(blocks: Content[], title: string, dataUrl: string | undefined): void {
  if (!dataUrl) return;
  blocks.push(
    { text: title, fontSize: 12, bold: true, margin: m(0, 0, 6, 0) },
    { image: dataUrl, width: IMG_WIDTH, alignment: 'center', margin: m(0, 0, 14, 0) }
  );
}

function buildContent(
  data: DashboardPdfData,
  captures?: DashboardPdfScreenCaptures,
  period?: DashboardPdfFilterPeriod
): Content {
  const generated = new Date().toLocaleString('ru-RU', { dateStyle: 'long', timeStyle: 'short' });

  const periodLabel = (period?.from || period?.to)
    ? `Период фильтра: ${period.from ?? '...'} — ${period.to ?? '...'}`
    : 'Период: всё время';

  const statTableBody = [
    [
      { text: 'Показатель', bold: true, fillColor: '#eeeeee' },
      { text: 'Значение',   bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
    ],
    ...STAT_ROWS.map(({ key, label }) => [
      { text: label },
      { text: nf(data[key] as number), alignment: 'right' as const },
    ]),
  ];

  const statusBody = [
    [
      { text: 'Код',    bold: true, fillColor: '#eeeeee' },
      { text: 'Статус', bold: true, fillColor: '#eeeeee' },
      { text: 'Кол-во', bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
    ],
    ...[...data.statusDistribution]
      .sort((a, b) => a.status - b.status)
      .map(s => [
        { text: String(s.status) },
        { text: STATUS_LABELS[s.status] ?? `Статус ${s.status}` },
        { text: nf(s.count), alignment: 'right' as const },
      ]),
  ];

  const opsBody = [
    [
      { text: 'Оператор',  bold: true, fillColor: '#eeeeee' },
      { text: 'Суспенсов', bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
      { text: 'Выручка',   bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
    ],
    ...data.topOperators.map(o => [
      { text: o.operator || '—' },
      { text: nf(o.count),    alignment: 'right' as const },
      { text: money(o.revenue), alignment: 'right' as const },
    ]),
  ];

  const terrBody = [
    [
      { text: 'Территория', bold: true, fillColor: '#eeeeee' },
      { text: 'Суспенсов',  bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
      { text: 'Выручка',    bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
    ],
    ...data.topTerritories.map(t => [
      { text: t.territoryCode || '—' },
      { text: nf(t.count),    alignment: 'right' as const },
      { text: money(t.revenue), alignment: 'right' as const },
    ]),
  ];

  const compBody = [
    [
      { text: 'Компания',   bold: true, fillColor: '#eeeeee' },
      { text: 'Суспенсов', bold: true, fillColor: '#eeeeee', alignment: 'right' as const },
    ],
    ...data.topCompanies.map(c => [
      { text: c.companyName || '—' },
      { text: nf(c.count), alignment: 'right' as const },
    ]),
  ];

  const blocks: Content[] = [
    { text: 'Дашборд SuspenseManager', fontSize: 18, bold: true, margin: m(0, 0, 6, 0) },
    { text: `Сформировано: ${generated}`, fontSize: 9, color: '#666666', margin: m(0, 0, 2, 0) },
    { text: periodLabel, fontSize: 9, color: '#444444', italics: true, margin: m(0, 0, 14, 0) },
  ];

  // Screen captures
  const hasCharts =
    !!captures?.statCards || !!captures?.summary || !!captures?.donut ||
    !!captures?.bar || !!captures?.revBar || !!captures?.terrBar || !!captures?.compBar;

  if (hasCharts) {
    blocks.push({
      text: 'Снимки экрана дашборда',
      fontSize: 13, bold: true, margin: m(0, 0, 8, 0),
    });
    pushScreenshot(blocks, 'Карточки показателей',                 captures?.statCards);
    pushScreenshot(blocks, 'Сводка: записи, стримы, выручка',     captures?.summary);
    pushScreenshot(blocks, 'Структура обращений по статусам',      captures?.donut);
    pushScreenshot(blocks, 'Операторы: объём входящих записей',    captures?.bar);
    pushScreenshot(blocks, 'Операторы: выручка к распределению',   captures?.revBar);
    pushScreenshot(blocks, 'Территории: входящие обращения',       captures?.terrBar);
    pushScreenshot(blocks, 'Правообладатели: входящие обращения',  captures?.compBar);
  }

  // Numerical tables (always present as backup / additional data)
  blocks.push(
    { text: 'Ключевые показатели', fontSize: 13, bold: true, margin: m(0, 0, 8, 0) },
    {
      table: {
        widths: ['*', 100],
        body: [
          [{ text: 'Всего записей' }, { text: nf(data.totalSuspenses), alignment: 'right' as const }],
          [{ text: 'Стримов' },         { text: nf(data.totalStreams),    alignment: 'right' as const }],
          [{ text: 'Выручка (₽)' },     { text: money(data.totalRevenue), alignment: 'right' as const }],
        ],
      },
      layout: 'lightHorizontalLines',
      margin: m(0, 0, 16, 0),
    }
  );

  if (!captures?.statCards) {
    blocks.push(
      { text: 'Статусы обращений', fontSize: 13, bold: true, margin: m(0, 0, 8, 0) },
      { table: { widths: ['*', 80], body: statTableBody }, layout: 'lightHorizontalLines', margin: m(0, 0, 16, 0) }
    );
  }

  if (!captures?.donut) {
    blocks.push({ text: 'Структура обращений по статусам', fontSize: 13, bold: true, margin: m(0, 0, 8, 0) });
    if (data.statusDistribution.length === 0) {
      blocks.push({ text: 'Нет данных', italics: true, margin: m(0, 0, 16, 0) });
    } else {
      blocks.push({ table: { widths: [40, '*', 70], body: statusBody }, layout: 'lightHorizontalLines', margin: m(0, 0, 16, 0) });
    }
  }

  if (!captures?.bar || !captures?.revBar) {
    blocks.push({ text: 'Операторы', fontSize: 13, bold: true, margin: m(0, 0, 8, 0) });
    if (data.topOperators.length === 0) {
      blocks.push({ text: 'Нет данных', italics: true, margin: m(0, 0, 16, 0) });
    } else {
      blocks.push({ table: { widths: ['*', 70, 90], body: opsBody }, layout: 'lightHorizontalLines', margin: m(0, 0, 16, 0) });
    }
  }

  if (!captures?.terrBar) {
    blocks.push({ text: 'Территории', fontSize: 13, bold: true, margin: m(0, 0, 8, 0) });
    if (data.topTerritories.length === 0) {
      blocks.push({ text: 'Нет данных', italics: true, margin: m(0, 0, 16, 0) });
    } else {
      blocks.push({ table: { widths: ['*', 70, 90], body: terrBody }, layout: 'lightHorizontalLines', margin: m(0, 0, 16, 0) });
    }
  }

  if (!captures?.compBar) {
    blocks.push({ text: 'Правообладатели', fontSize: 13, bold: true, margin: m(0, 0, 8, 0) });
    if (data.topCompanies.length === 0) {
      blocks.push({ text: 'Нет данных', italics: true, margin: m(0, 0, 16, 0) });
    } else {
      blocks.push({ table: { widths: ['*', 70], body: compBody }, layout: 'lightHorizontalLines', margin: m(0, 0, 16, 0) });
    }
  }

  return blocks;
}

export function downloadDashboardPdf(
  data: DashboardPdfData,
  captures?: DashboardPdfScreenCaptures,
  period?: DashboardPdfFilterPeriod
): Promise<void> {
  const dd: TDocumentDefinitions = {
    content: buildContent(data, captures, period),
    defaultStyle: { font: 'Roboto', fontSize: 10 },
    pageMargins: [40, 48, 40, 48] as [number, number, number, number],
  };
  const stamp = new Date().toISOString().slice(0, 10);
  const periodSuffix = period?.from ? `_${period.from}_${period.to ?? 'now'}` : '';
  return pdfMake.createPdf(dd).download(`suspensemanager-dashboard-${stamp}${periodSuffix}.pdf`);
}
