// Tappable venue address that opens the location in maps.
import React from 'react';
import { Linking, Pressable, Text } from 'react-native';
import { Theme, fonts } from '../theme';
import { VENUE } from '../venue';

export function AddressLink({
  theme: t,
  size = 13,
  color,
}: {
  theme: Theme;
  size?: number;
  color?: string;
}) {
  return (
    <Pressable
      onPress={() => Linking.openURL(VENUE.mapUrl).catch(() => {})}
      hitSlop={6}
    >
      <Text
        style={{
          fontFamily: fonts.inter500,
          fontSize: size,
          lineHeight: size + 5,
          color: color ?? t.volt,
          textDecorationLine: 'underline',
        }}
      >
        {VENUE.address}
      </Text>
    </Pressable>
  );
}
