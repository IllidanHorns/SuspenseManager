import { Box, Text } from '@mantine/core';

const svgWrap = { width: '100%', maxWidth: 640, margin: '0 auto', display: 'block' as const };

/** Отчёт → загрузка → валидация → три исхода. */
export function DiagramUploadValidateFlow() {
  return (
    <Box>
      <svg viewBox="0 0 560 200" style={svgWrap} aria-hidden>
        <defs>
          <marker id="arrow-kb" markerWidth="8" markerHeight="8" refX="6" refY="4" orient="auto">
            <path d="M0,0 L8,4 L0,8 Z" fill="var(--mantine-color-dimmed)" />
          </marker>
        </defs>
        <g fontFamily="system-ui, sans-serif" fontSize="11" fill="var(--mantine-color-text)">
          <rect x="8" y="78" width="72" height="36" rx="8" fill="var(--mantine-color-gray-1)" stroke="var(--mantine-color-gray-5)" />
          <text x="44" y="100" textAnchor="middle">
            Отчёт
          </text>
          <line x1="80" y1="96" x2="108" y2="96" stroke="var(--mantine-color-dimmed)" strokeWidth="1.5" markerEnd="url(#arrow-kb)" />
          <rect x="108" y="78" width="88" height="36" rx="8" fill="var(--mantine-color-gray-1)" stroke="var(--mantine-color-gray-5)" />
          <text x="152" y="96" textAnchor="middle">
            Загрузка
          </text>
          <line x1="196" y1="96" x2="224" y2="96" stroke="var(--mantine-color-dimmed)" strokeWidth="1.5" markerEnd="url(#arrow-kb)" />
          <rect x="224" y="72" width="100" height="48" rx="8" fill="var(--mantine-color-blue-0)" stroke="var(--mantine-color-blue-4)" />
          <text x="274" y="92" textAnchor="middle" fontWeight="600">
            Валидация
          </text>
          <text x="274" y="108" textAnchor="middle" fontSize="10" fill="var(--mantine-color-dimmed)">
            каталог + права
          </text>
          <line x1="274" y1="120" x2="274" y2="138" stroke="var(--mantine-color-dimmed)" strokeWidth="1.5" />
          <line x1="274" y1="138" x2="274" y2="150" stroke="var(--mantine-color-dimmed)" strokeWidth="1.5" />
          <line x1="120" y1="138" x2="430" y2="138" stroke="var(--mantine-color-dimmed)" strokeWidth="1.5" />
          <line x1="120" y1="138" x2="120" y2="150" stroke="var(--mantine-color-dimmed)" strokeWidth="1.5" />
          <line x1="274" y1="138" x2="274" y2="150" stroke="var(--mantine-color-dimmed)" strokeWidth="1.5" />
          <line x1="430" y1="138" x2="430" y2="150" stroke="var(--mantine-color-dimmed)" strokeWidth="1.5" />
          <rect x="56" y="152" width="128" height="40" rx="8" fill="var(--mantine-color-indigo-0)" stroke="var(--mantine-color-indigo-5)" />
          <text x="120" y="170" textAnchor="middle" fontWeight="600" fill="var(--mantine-color-indigo-8)">
            Нет продукта (0)
          </text>
          <text x="120" y="184" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            ветка каталога
          </text>
          <rect x="210" y="152" width="128" height="40" rx="8" fill="var(--mantine-color-teal-0)" stroke="var(--mantine-color-teal-5)" />
          <text x="274" y="170" textAnchor="middle" fontWeight="600" fill="var(--mantine-color-teal-8)">
            Нет прав (1)
          </text>
          <text x="274" y="184" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            ветка прав
          </text>
          <rect x="364" y="152" width="128" height="40" rx="8" fill="var(--mantine-color-green-0)" stroke="var(--mantine-color-green-5)" />
          <text x="428" y="174" textAnchor="middle" fontWeight="600" fill="var(--mantine-color-green-8)">
            Успех (88)
          </text>
        </g>
      </svg>
      <Text size="xs" c="dimmed" ta="center" mt={6}>
        Исход валидации задаёт ветку дальнейшей работы или сразу закрывает строку.
      </Text>
    </Box>
  );
}

/** Ветка «нет продукта»: 0 → 15 → исходы. */
export function DiagramNoProductBranchFlow() {
  return (
    <Box>
      <svg viewBox="0 0 400 280" style={{ ...svgWrap, maxWidth: 400 }} aria-hidden>
        <defs>
          <marker id="arrow-np" markerWidth="8" markerHeight="8" refX="6" refY="4" orient="auto">
            <path d="M0,0 L8,4 L0,8 Z" fill="var(--mantine-color-indigo-5)" />
          </marker>
        </defs>
        <g fontFamily="system-ui, sans-serif" fontSize="11" fill="var(--mantine-color-text)">
          <rect x="140" y="8" width="120" height="36" rx="8" fill="var(--mantine-color-indigo-0)" stroke="var(--mantine-color-indigo-5)" />
          <text x="200" y="30" textAnchor="middle" fontWeight="600">
            Строки 0
          </text>
          <line x1="200" y1="44" x2="200" y2="58" stroke="var(--mantine-color-indigo-5)" strokeWidth="1.5" markerEnd="url(#arrow-np)" />
          <rect x="120" y="58" width="160" height="40" rx="8" fill="var(--mantine-color-indigo-1)" stroke="var(--mantine-color-indigo-6)" />
          <text x="200" y="76" textAnchor="middle" fontWeight="600">
            Группировка → 15
          </text>
          <text x="200" y="90" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            активная группа
          </text>
          <line x1="200" y1="98" x2="200" y2="112" stroke="var(--mantine-color-indigo-5)" strokeWidth="1.5" markerEnd="url(#arrow-np)" />
          <rect x="100" y="112" width="200" height="44" rx="8" fill="var(--mantine-color-body)" stroke="var(--mantine-color-indigo-4)" strokeDasharray="4 3" />
          <text x="200" y="132" textAnchor="middle" fontWeight="600">
            Каталог: новый продукт / привязка
          </text>
          <text x="200" y="148" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            при появлении продукта часто → 16
          </text>
          <line x1="200" y1="156" x2="200" y2="172" stroke="var(--mantine-color-indigo-5)" strokeWidth="1.5" />
          <line x1="80" y1="172" x2="320" y2="172" stroke="var(--mantine-color-indigo-5)" strokeWidth="1.5" />
          <line x1="80" y1="172" x2="80" y2="184" stroke="var(--mantine-color-indigo-5)" strokeWidth="1.5" />
          <line x1="200" y1="172" x2="200" y2="184" stroke="var(--mantine-color-indigo-5)" strokeWidth="1.5" />
          <line x1="320" y1="172" x2="320" y2="184" stroke="var(--mantine-color-indigo-5)" strokeWidth="1.5" />
          <rect x="20" y="186" width="120" height="36" rx="8" fill="var(--mantine-color-gray-0)" stroke="var(--mantine-color-gray-5)" />
          <text x="80" y="208" textAnchor="middle" fontSize="10">
            Отложить 30
          </text>
          <rect x="140" y="186" width="120" height="36" rx="8" fill="var(--mantine-color-teal-0)" stroke="var(--mantine-color-teal-5)" />
          <text x="200" y="208" textAnchor="middle" fontSize="10" fontWeight="600">
            Права 16 → 88
          </text>
          <rect x="260" y="186" width="120" height="36" rx="8" fill="var(--mantine-color-gray-0)" stroke="var(--mantine-color-gray-5)" />
          <text x="320" y="208" textAnchor="middle" fontSize="10">
            Бэк-офис 120
          </text>
          <text x="200" y="248" textAnchor="middle" fontSize="10" fill="var(--mantine-color-dimmed)">
            Унгруппировка возвращает к 0 (без смешения с веткой «нет прав»).
          </text>
        </g>
      </svg>
    </Box>
  );
}

/** Ветка «нет прав»: 1 → 16 → исходы. */
export function DiagramNoRightsBranchFlow() {
  return (
    <Box>
      <svg viewBox="0 0 400 260" style={{ ...svgWrap, maxWidth: 400 }} aria-hidden>
        <defs>
          <marker id="arrow-nr" markerWidth="8" markerHeight="8" refX="6" refY="4" orient="auto">
            <path d="M0,0 L8,4 L0,8 Z" fill="var(--mantine-color-teal-5)" />
          </marker>
        </defs>
        <g fontFamily="system-ui, sans-serif" fontSize="11" fill="var(--mantine-color-text)">
          <rect x="140" y="8" width="120" height="36" rx="8" fill="var(--mantine-color-teal-0)" stroke="var(--mantine-color-teal-5)" />
          <text x="200" y="30" textAnchor="middle" fontWeight="600">
            Строки 1
          </text>
          <line x1="200" y1="44" x2="200" y2="58" stroke="var(--mantine-color-teal-5)" strokeWidth="1.5" markerEnd="url(#arrow-nr)" />
          <rect x="110" y="58" width="180" height="44" rx="8" fill="var(--mantine-color-teal-1)" stroke="var(--mantine-color-teal-6)" />
          <text x="200" y="78" textAnchor="middle" fontWeight="600">
            Группировка → 16
          </text>
          <text x="200" y="94" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            продукт из каталога
          </text>
          <line x1="200" y1="102" x2="200" y2="118" stroke="var(--mantine-color-teal-5)" strokeWidth="1.5" markerEnd="url(#arrow-nr)" />
          <rect x="70" y="118" width="260" height="44" rx="8" fill="var(--mantine-color-body)" stroke="var(--mantine-color-teal-4)" strokeDasharray="4 3" />
          <text x="200" y="138" textAnchor="middle" fontWeight="600">
            Метаправа, договор, территория
          </text>
          <text x="200" y="154" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            валидация → закрытие
          </text>
          <line x1="200" y1="162" x2="200" y2="176" stroke="var(--mantine-color-teal-5)" strokeWidth="1.5" />
          <line x1="100" y1="176" x2="300" y2="176" stroke="var(--mantine-color-teal-5)" strokeWidth="1.5" />
          <line x1="100" y1="176" x2="100" y2="188" stroke="var(--mantine-color-teal-5)" strokeWidth="1.5" />
          <line x1="200" y1="176" x2="200" y2="188" stroke="var(--mantine-color-teal-5)" strokeWidth="1.5" />
          <line x1="300" y1="176" x2="300" y2="188" stroke="var(--mantine-color-teal-5)" strokeWidth="1.5" />
          <rect x="30" y="190" width="140" height="36" rx="8" fill="var(--mantine-color-green-0)" stroke="var(--mantine-color-green-5)" />
          <text x="100" y="212" textAnchor="middle" fontWeight="600" fontSize="10">
            Успех 88
          </text>
          <rect x="130" y="190" width="140" height="36" rx="8" fill="var(--mantine-color-gray-0)" stroke="var(--mantine-color-gray-5)" />
          <text x="200" y="212" textAnchor="middle" fontSize="10">
            Отложить 32
          </text>
          <rect x="230" y="190" width="140" height="36" rx="8" fill="var(--mantine-color-gray-0)" stroke="var(--mantine-color-gray-5)" />
          <text x="300" y="212" textAnchor="middle" fontSize="10">
            Бэк-офис 320
          </text>
        </g>
      </svg>
    </Box>
  );
}

/** Предпросмотр → коммит одной группы. */
export function DiagramGroupingFlow() {
  return (
    <Box>
      <svg viewBox="0 0 480 100" style={{ ...svgWrap, maxWidth: 480 }} aria-hidden>
        <defs>
          <marker id="arrow-gr" markerWidth="8" markerHeight="8" refX="6" refY="4" orient="auto">
            <path d="M0,0 L8,4 L0,8 Z" fill="var(--mantine-color-violet-5)" />
          </marker>
        </defs>
        <g fontFamily="system-ui, sans-serif" fontSize="11" fill="var(--mantine-color-text)">
          <rect x="8" y="28" width="100" height="44" rx="8" fill="var(--mantine-color-violet-0)" stroke="var(--mantine-color-violet-5)" />
          <text x="58" y="48" textAnchor="middle" fontWeight="600">
            Строки
          </text>
          <text x="58" y="62" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            0 или 1
          </text>
          <line x1="108" y1="50" x2="132" y2="50" stroke="var(--mantine-color-violet-5)" strokeWidth="1.5" markerEnd="url(#arrow-gr)" />
          <rect x="132" y="22" width="120" height="56" rx="8" fill="var(--mantine-color-body)" stroke="var(--mantine-color-violet-4)" />
          <text x="192" y="42" textAnchor="middle" fontWeight="600">
            Предпросмотр
          </text>
          <text x="192" y="58" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            GROUP BY + COUNT
          </text>
          <text x="192" y="72" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            фильтры, сортировка
          </text>
          <line x1="252" y1="50" x2="276" y2="50" stroke="var(--mantine-color-violet-5)" strokeWidth="1.5" markerEnd="url(#arrow-gr)" />
          <rect x="276" y="28" width="100" height="44" rx="8" fill="var(--mantine-color-violet-1)" stroke="var(--mantine-color-violet-6)" />
          <text x="326" y="48" textAnchor="middle" fontWeight="600">
            Коммит
          </text>
          <text x="326" y="62" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            одна группа
          </text>
          <line x1="376" y1="50" x2="400" y2="50" stroke="var(--mantine-color-violet-5)" strokeWidth="1.5" markerEnd="url(#arrow-gr)" />
          <rect x="400" y="24" width="72" height="52" rx="8" fill="var(--mantine-color-indigo-0)" stroke="var(--mantine-color-indigo-5)" />
          <text x="436" y="44" textAnchor="middle" fontSize="10" fontWeight="600">
            15
          </text>
          <text x="436" y="58" textAnchor="middle" fontSize="9" fill="var(--mantine-color-dimmed)">
            или
          </text>
          <text x="436" y="70" textAnchor="middle" fontSize="10" fontWeight="600">
            16
          </text>
        </g>
      </svg>
      <Text size="xs" c="dimmed" ta="center" mt={4}>
        Код 15 или 16 зависит только от исходной ветки (0 или 1), не от смешения полей.
      </Text>
    </Box>
  );
}
