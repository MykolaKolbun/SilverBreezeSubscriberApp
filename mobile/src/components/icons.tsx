// Lucide-style 1.5px-stroke icons, hand-written to match the prototype SVGs.
// 24px artboard, stroke = currentColor equivalent passed via `color`.
import React from 'react';
import Svg, { Circle, Line, Path, Polyline, Rect } from 'react-native-svg';

interface IconProps {
  size?: number;
  color: string;
}

const base = (size: number) => ({
  width: size,
  height: size,
  viewBox: '0 0 24 24',
  fill: 'none' as const,
  strokeWidth: 1.5,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
});

export const ChevronLeft = ({ size = 20, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Polyline points="15 18 9 12 15 6" />
  </Svg>
);

export const ChevronRight = ({ size = 18, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Polyline points="9 18 15 12 9 6" />
  </Svg>
);

/** Card / pass icon — payment method row and the Pass tab. */
export const CardIcon = ({ size = 22, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Rect x={2} y={5} width={20} height={14} rx={2} />
    <Line x1={2} y1={10} x2={22} y2={10} />
  </Svg>
);

export const UserIcon = ({ size = 22, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
    <Circle cx={12} cy={7} r={4} />
  </Svg>
);

export const LockIcon = ({ size = 12, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Rect x={3} y={11} width={18} height={11} rx={2} />
    <Path d="M7 11V7a5 5 0 0 1 10 0v4" />
  </Svg>
);

export const CarIcon = ({ size = 20, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Path d="M5 17h14M5 17a2 2 0 1 1-4 0 2 2 0 0 1 4 0zm14 0a2 2 0 1 0 4 0 2 2 0 0 0-4 0zM3 17V11l2-5h10l4 5v6M5 11h14" />
  </Svg>
);

export const PlusIcon = ({ size = 28, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Line x1={12} y1={5} x2={12} y2={19} />
    <Line x1={5} y1={12} x2={19} y2={12} />
  </Svg>
);

export const PhoneIcon = ({ size = 18, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07 19.5 19.5 0 0 1-6-6 19.79 19.79 0 0 1-3.07-8.67A2 2 0 0 1 4.11 2h3a2 2 0 0 1 2 1.72c.13.96.37 1.9.72 2.81a2 2 0 0 1-.45 2.11L8.09 9.91a16 16 0 0 0 6 6l1.27-1.27a2 2 0 0 1 2.11-.45c.9.35 1.85.59 2.81.72A2 2 0 0 1 22 16.92z" />
  </Svg>
);

export const MailIcon = ({ size = 18, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z" />
    <Polyline points="22,6 12,13 2,6" />
  </Svg>
);

export const BellIcon = ({ size = 18, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
    <Path d="M13.73 21a2 2 0 0 1-3.46 0" />
  </Svg>
);

export const SunIcon = ({ size = 16, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Circle cx={12} cy={12} r={4} />
    <Path d="M12 2v2M12 20v2M4.93 4.93l1.41 1.41M17.66 17.66l1.41 1.41M2 12h2M20 12h2M4.93 19.07l1.41-1.41M17.66 6.34l1.41-1.41" />
  </Svg>
);

export const MoonIcon = ({ size = 16, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Path d="M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 0 21 12.79z" />
  </Svg>
);

export const SignOutIcon = ({ size = 18, color }: IconProps) => (
  <Svg {...base(size)} stroke={color}>
    <Path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
    <Polyline points="16 17 21 12 16 7" />
    <Line x1={21} y1={12} x2={9} y2={12} />
  </Svg>
);

/** Apple logo (filled) for the Apple Pay swatch. */
export const AppleIcon = ({ size = 16, color }: IconProps) => (
  <Svg width={size} height={size} viewBox="0 0 24 24" fill={color}>
    <Path d="M17.05 12.54c-.03-2.89 2.36-4.28 2.47-4.35-1.35-1.97-3.44-2.24-4.18-2.27-1.78-.18-3.47 1.05-4.37 1.05-.9 0-2.29-1.02-3.76-1-1.94.03-3.72 1.13-4.72 2.86-2.01 3.49-.51 8.66 1.45 11.49.96 1.39 2.1 2.94 3.6 2.88 1.45-.06 2-.93 3.74-.93 1.75 0 2.24.93 3.77.9 1.56-.03 2.54-1.41 3.49-2.8 1.1-1.61 1.55-3.17 1.58-3.25-.04-.02-3.03-1.16-3.07-4.58zM14.16 4.06c.8-.97 1.34-2.32 1.19-3.66-1.15.05-2.55.77-3.38 1.73-.74.86-1.39 2.23-1.22 3.55 1.29.1 2.6-.65 3.41-1.62z" />
  </Svg>
);
