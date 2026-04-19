import * as pdfMakeNs from 'pdfmake/build/pdfmake';
import pdfFonts from 'pdfmake/build/vfs_fonts';
import type { Content, TDocumentDefinitions } from 'pdfmake/interfaces';
import { GLOSSARY_ENTRIES, STATUS_TEXT_ENTRIES } from '../data/knowledgeBaseData';
import { STATUS_LABELS } from '../types';

/** Vite ESM: singleton на `default`, иначе на корне namespace */
const pdfMake = (pdfMakeNs as { default?: typeof pdfMakeNs }).default ?? pdfMakeNs;

pdfMake.addVirtualFileSystem(pdfFonts);

const titleStyle = { fontSize: 18, bold: true, margin: [0, 0, 0, 12] as [number, number, number, number] };
const h2Style = { fontSize: 13, bold: true, margin: [0, 14, 0, 6] as [number, number, number, number] };
const muted = { fontSize: 9, color: '#666666', margin: [0, 0, 0, 16] as [number, number, number, number] };

function buildStatusesContent(): Content {
  const blocks: Content[] = [
    { text: 'Описание статусов', style: 'docTitle' },
    {
      text: 'SuspenseManager — справочник кодов статусов. Код совпадает с таблицами и фильтрами в системе.',
      style: 'muted',
    },
  ];

  for (const e of STATUS_TEXT_ENTRIES) {
    const label = STATUS_LABELS[e.code] ?? `Статус ${e.code}`;
    blocks.push(
      { text: `${e.code} — ${label}`, style: 'h2' },
      { text: e.shortTitle, italics: true, margin: [0, 0, 0, 6] },
      { text: e.summary, margin: [0, 0, 0, 6] },
      { text: e.detail, margin: [0, 0, 0, 8] },
      {
        text: [{ text: 'Пример: ', bold: true }, { text: e.example }],
        margin: [0, 0, 0, 16],
      }
    );
  }

  return blocks;
}

function buildGlossaryContent(): Content {
  const blocks: Content[] = [
    { text: 'Словарь терминов', style: 'docTitle' },
    {
      text: 'Краткие определения в алфавитном порядке. Формулировки облегчают чтение статусов и отчётов.',
      style: 'muted',
    },
  ];

  for (const e of GLOSSARY_ENTRIES) {
    blocks.push(
      { text: e.term, style: 'h2' },
      { text: e.definition, margin: [0, 0, 0, 14] }
    );
  }

  return blocks;
}

const sharedStyles = {
  docTitle: titleStyle,
  h2: h2Style,
  muted,
};

export function downloadKnowledgeStatusesPdf(): Promise<void> {
  const dd: TDocumentDefinitions = {
    content: buildStatusesContent(),
    styles: sharedStyles,
    defaultStyle: { font: 'Roboto', fontSize: 10 },
    pageMargins: [40, 48, 40, 48] as [number, number, number, number],
  };
  return pdfMake.createPdf(dd).download('suspensemanager-statusy.pdf');
}

export function downloadKnowledgeGlossaryPdf(): Promise<void> {
  const dd: TDocumentDefinitions = {
    content: buildGlossaryContent(),
    styles: sharedStyles,
    defaultStyle: { font: 'Roboto', fontSize: 10 },
    pageMargins: [40, 48, 40, 48] as [number, number, number, number],
  };
  return pdfMake.createPdf(dd).download('suspensemanager-slovar.pdf');
}
