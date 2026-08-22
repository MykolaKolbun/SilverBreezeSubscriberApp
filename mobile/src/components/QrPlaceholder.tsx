// Decorative QR-style graphic copied from the prototype. NOT a scannable
// code — wire a real QR encoder (e.g. of the pass/entry token from
// GET /parking-cards/{id}/qr) before production.
import React from 'react';
import Svg, { Rect } from 'react-native-svg';

const INK = '#0F1117';

// x, y, w, h of every dark module in the placeholder artwork.
const CELLS: [number, number, number, number][] = [
  [16, 16, 16, 16], [100, 16, 16, 16], [16, 100, 16, 16],
  [50, 6, 8, 8], [66, 6, 8, 8], [50, 18, 8, 8], [66, 30, 8, 8],
  [50, 42, 8, 8], [66, 54, 8, 8], [50, 66, 8, 8], [66, 66, 8, 8],
  [6, 50, 8, 8], [18, 58, 8, 8], [30, 50, 8, 8], [6, 66, 8, 8],
  [30, 70, 8, 8], [90, 50, 8, 8], [106, 50, 8, 8], [118, 58, 8, 8],
  [90, 66, 8, 8], [106, 74, 8, 8], [90, 90, 8, 8], [106, 90, 8, 8],
  [118, 98, 8, 8], [90, 106, 8, 8], [106, 118, 8, 8], [50, 90, 8, 8],
  [66, 98, 8, 8], [50, 106, 8, 8], [66, 118, 8, 8], [42, 42, 8, 8],
  [82, 42, 8, 8], [42, 82, 8, 8],
];

// The three finder-pattern outer squares (stroked, not filled).
const FINDERS: [number, number][] = [
  [6, 6],
  [90, 6],
  [6, 90],
];

export function QrPlaceholder({ size = 132 }: { size?: number }) {
  return (
    <Svg width={size} height={size} viewBox="0 0 132 132">
      <Rect x={0} y={0} width={132} height={132} fill="#FFFFFF" />
      {FINDERS.map(([x, y]) => (
        <Rect
          key={`f${x}-${y}`}
          x={x}
          y={y}
          width={36}
          height={36}
          fill="none"
          stroke={INK}
          strokeWidth={6}
        />
      ))}
      {CELLS.map(([x, y, w, h]) => (
        <Rect key={`c${x}-${y}`} x={x} y={y} width={w} height={h} fill={INK} />
      ))}
    </Svg>
  );
}
