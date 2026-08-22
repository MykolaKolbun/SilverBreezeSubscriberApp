// Native date picker input for the "Start date" row.
// Native platforms use @react-native-community/datetimepicker; on web
// (Expo web / react-native-web) a plain DOM <input type="date"> matches
// the prototype exactly.
import React, { useState } from 'react';
import { Platform, Pressable, Text } from 'react-native';
import DateTimePicker from '@react-native-community/datetimepicker';
import { Theme, fonts } from '../theme';
import { fmtDate, toLocalISO } from '../plans';

interface Props {
  theme: Theme;
  value: string; // local YYYY-MM-DD
  onChange: (iso: string) => void;
}

export function DateField({ theme, value, onChange }: Props) {
  const [open, setOpen] = useState(false);

  if (Platform.OS === 'web') {
    return React.createElement('input', {
      type: 'date',
      value,
      onChange: (e: { target: { value: string } }) => onChange(e.target.value),
      style: {
        height: 40,
        padding: '0 10px',
        background: theme.surface2,
        border: `1.5px solid ${theme.border}`,
        borderRadius: 12,
        color: theme.fg1,
        fontFamily: fonts.inter500,
        fontSize: 13,
        outline: 'none',
        colorScheme: theme.name, // native picker chrome follows the theme
      },
    });
  }

  return (
    <>
      <Pressable
        onPress={() => setOpen(true)}
        style={{
          height: 40,
          paddingHorizontal: 12,
          backgroundColor: theme.surface2,
          borderWidth: 1.5,
          borderColor: theme.border,
          borderRadius: 12,
          justifyContent: 'center',
        }}
      >
        <Text
          style={{
            fontFamily: fonts.inter500,
            fontSize: 13,
            lineHeight: 18,
            color: theme.fg1,
          }}
        >
          {fmtDate(value)}
        </Text>
      </Pressable>
      {open && (
        <DateTimePicker
          value={new Date(value + 'T00:00:00')}
          mode="date"
          onChange={(_event, date) => {
            setOpen(Platform.OS === 'ios');
            if (date) onChange(toLocalISO(date));
          }}
        />
      )}
    </>
  );
}
