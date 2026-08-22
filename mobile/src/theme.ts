// Voltway design tokens (see design_handoff_parking_pass_app/README.md).
// The RN equivalent of the prototype's CSS custom properties: components
// receive one of these palettes via ThemeContext instead of var(--token).

export type ThemeName = 'dark' | 'light';

export interface Theme {
  name: ThemeName;
  bg: string;
  bgElevated: string;
  surface2: string;
  border: string;
  borderStrong: string;
  fg1: string;
  fg2: string;
  fg3: string;
  volt: string;
  voltSoft: string;
  voltDeep: string;
  danger: string;
}

export const themes: Record<ThemeName, Theme> = {
  dark: {
    name: 'dark',
    bg: '#0F1117',
    bgElevated: '#171A22',
    surface2: '#1F2330',
    border: '#252A38',
    borderStrong: '#363C4E',
    fg1: '#F2F4F8',
    fg2: '#A4ABBD',
    fg3: '#6A7187',
    volt: '#22E07A',
    voltSoft: 'rgba(34,224,122,0.14)',
    voltDeep: '#0FB45F',
    danger: '#FF6B6B',
  },
  light: {
    name: 'light',
    bg: '#F4F6FA',
    bgElevated: '#FFFFFF',
    surface2: '#FFFFFF',
    border: '#E3E7EE',
    borderStrong: '#CFD5E0',
    fg1: '#0F1117',
    fg2: '#5B6478',
    fg3: '#8A92A4',
    volt: '#22E07A',
    voltSoft: 'rgba(34,224,122,0.14)',
    voltDeep: '#0FB45F',
    danger: '#FF6B6B',
  },
};

// Text placed on top of the volt green is always ink-dark, in both themes.
export const ON_VOLT = '#0F1117';

// Font family names as registered by the @expo-google-fonts packages.
export const fonts = {
  grotesk600: 'SpaceGrotesk_600SemiBold',
  grotesk700: 'SpaceGrotesk_700Bold',
  inter400: 'Inter_400Regular',
  inter500: 'Inter_500Medium',
  inter600: 'Inter_600SemiBold',
  inter700: 'Inter_700Bold',
  mono500: 'JetBrainsMono_500Medium',
  mono700: 'JetBrainsMono_700Bold',
} as const;

export const radius = {
  sm: 12, // inputs / small buttons
  md: 16, // cards
  lg: 24, // hero cards / primary CTA
  pill: 999,
} as const;

// Signature "live pulse": 2s loop, this easing, opacity 1→0.55, scale 1→0.85.
// Reuse for any live indicator — never vary the timing.
export const pulseEasing = [0.2, 0.8, 0.2, 1] as const;
