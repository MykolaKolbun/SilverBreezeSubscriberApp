// Floating pill nav — two tabs (Pass / Profile), visible on every screen.
// Deliberately dark/translucent in both themes, matching the prototype.
import React from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { BlurView } from 'expo-blur';
import { fonts } from '../theme';
import { useApp } from '../state';
import { CardIcon, UserIcon } from './icons';

const INACTIVE = '#6A7187';

export function BottomNav() {
  const { theme, screen, setScreen } = useApp();

  const tabs = [
    { id: 'pass' as const, label: 'Pass', Icon: CardIcon },
    { id: 'profile' as const, label: 'Profile', Icon: UserIcon },
  ];

  return (
    <View style={styles.wrap}>
      <BlurView intensity={40} tint="dark" style={StyleSheet.absoluteFill} />
      {tabs.map(({ id, label, Icon }) => {
        const color = screen === id ? theme.volt : INACTIVE;
        return (
          <Pressable key={id} onPress={() => setScreen(id)} style={styles.tab}>
            <Icon size={22} color={color} />
            <Text style={[styles.label, { color }]}>{label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    position: 'absolute',
    left: 12,
    right: 12,
    bottom: 22,
    height: 70,
    padding: 8,
    backgroundColor: 'rgba(15,17,23,0.82)',
    borderWidth: 1,
    borderColor: 'rgba(255,255,255,0.06)',
    borderRadius: 32,
    boxShadow: '0 12px 40px rgba(0,0,0,0.5)',
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-around',
    overflow: 'hidden',
    zIndex: 40,
  },
  tab: {
    flex: 1,
    alignItems: 'center',
    gap: 4,
    paddingVertical: 4,
    paddingHorizontal: 10,
  },
  label: {
    fontFamily: fonts.inter600,
    fontSize: 11,
    lineHeight: 14,
  },
});
