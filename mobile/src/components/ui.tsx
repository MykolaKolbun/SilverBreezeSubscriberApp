// Tiny shared primitives used across screens.
import React, { useEffect, useRef } from 'react';
import {
  Animated,
  Easing,
  Platform,
  StyleSheet,
  Text,
  TextStyle,
  View,
} from 'react-native';
import { Theme, fonts, pulseEasing } from '../theme';

/** 11px uppercase tracked section label ("LOCATION", "START DATE"…). */
export function Overline({
  theme,
  color,
  style,
  children,
}: {
  theme: Theme;
  color?: string;
  style?: TextStyle;
  children: React.ReactNode;
}) {
  return (
    <Text
      style={[
        {
          fontFamily: fonts.inter600,
          fontSize: 11,
          lineHeight: 14,
          letterSpacing: 0.88,
          textTransform: 'uppercase',
          color: color ?? theme.fg3,
        },
        style,
      ]}
    >
      {children}
    </Text>
  );
}

/** Radio dot used on plan and payment-method cards. */
export function Radio({ theme, active }: { theme: Theme; active: boolean }) {
  return (
    <View
      style={
        active
          ? {
              width: 22,
              height: 22,
              borderRadius: 11,
              backgroundColor: theme.volt,
            }
          : {
              width: 22,
              height: 22,
              borderRadius: 11,
              borderWidth: 2,
              borderColor: theme.borderStrong,
            }
      }
    />
  );
}

/** Selected/unselected card border + tint treatment (plans, pay methods). */
export function selectableCardStyle(theme: Theme, active: boolean) {
  return {
    backgroundColor: active ? theme.voltSoft : theme.bgElevated,
    borderWidth: active ? 1.5 : 1,
    borderColor: active ? theme.volt : theme.border,
    borderRadius: 16,
    padding: 18,
  } as const;
}

/**
 * The brand's signature "live pulse" dot: 2s loop, opacity 1→0.55,
 * scale 1→0.85, standard easing. Same timing for every live indicator.
 */
// On web, react-native-web compiles CSS keyframes — but only via
// StyleSheet.create, not inline styles — so the pulse is a real CSS
// animation there (same as the prototype) and an Animated loop natively.
const webPulseStyles =
  Platform.OS === 'web'
    ? StyleSheet.create({
        dot: {
          // @ts-expect-error react-native-web-only style props
          animationKeyframes: [
            {
              '0%': { opacity: 1, transform: 'scale(1)' },
              '50%': { opacity: 0.55, transform: 'scale(0.85)' },
              '100%': { opacity: 1, transform: 'scale(1)' },
            },
          ],
          animationDuration: '2s',
          animationIterationCount: 'infinite',
          animationTimingFunction: `cubic-bezier(${pulseEasing.join(',')})`,
        },
      })
    : null;

export function PulseDot({ color }: { color: string }) {
  const v = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (Platform.OS === 'web') return; // web uses a CSS animation below
    const anim = Animated.loop(
      Animated.sequence([
        Animated.timing(v, {
          toValue: 1,
          duration: 1000,
          easing: Easing.bezier(...pulseEasing),
          useNativeDriver: true,
        }),
        Animated.timing(v, {
          toValue: 0,
          duration: 1000,
          easing: Easing.bezier(...pulseEasing),
          useNativeDriver: true,
        }),
      ])
    );
    anim.start();
    return () => anim.stop();
  }, [v]);

  if (Platform.OS === 'web') {
    return (
      <View
        style={[
          { width: 8, height: 8, borderRadius: 4, backgroundColor: color },
          webPulseStyles!.dot,
        ]}
      />
    );
  }

  return (
    <Animated.View
      style={{
        width: 8,
        height: 8,
        borderRadius: 4,
        backgroundColor: color,
        opacity: v.interpolate({ inputRange: [0, 1], outputRange: [1, 0.55] }),
        transform: [
          { scale: v.interpolate({ inputRange: [0, 1], outputRange: [1, 0.85] }) },
        ],
      }}
    />
  );
}
