import { useCallback } from 'react';
import { Table } from '@mantine/core';

interface Props extends React.ThHTMLAttributes<HTMLTableCellElement> {
  children?: React.ReactNode;
  minWidth?: number;
}

export function ResizableTh({ children, minWidth = 60, style, ...props }: Props) {
  const onMouseDown = useCallback(
    (e: React.MouseEvent<HTMLSpanElement>) => {
      e.preventDefault();
      e.stopPropagation();
      const th = e.currentTarget.closest('th') as HTMLTableCellElement | null;
      if (!th) return;

      const startX = e.clientX;
      const startW = th.getBoundingClientRect().width;

      const onMove = (ev: MouseEvent) => {
        const w = Math.max(minWidth, startW + ev.clientX - startX);
        th.style.width = `${w}px`;
        th.style.minWidth = `${w}px`;
      };

      const onUp = () => {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
      };

      document.addEventListener('mousemove', onMove);
      document.addEventListener('mouseup', onUp);
    },
    [minWidth],
  );

  return (
    <Table.Th style={{ position: 'relative', userSelect: 'none', ...style }} {...(props as object)}>
      {children}
      <span
        onMouseDown={onMouseDown}
        onMouseEnter={(e) => { e.currentTarget.style.background = 'var(--mantine-color-blue-5)'; }}
        onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent'; }}
        style={{
          position: 'absolute',
          right: 0,
          top: '15%',
          bottom: '15%',
          width: 3,
          cursor: 'col-resize',
          borderRadius: 2,
          background: 'transparent',
          transition: 'background 120ms',
          zIndex: 1,
        }}
      />
    </Table.Th>
  );
}
