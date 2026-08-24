// Auth — sign in / register gate. Shown until there is a session.
import React, { useState } from 'react';
import {
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  Text,
  TextInput,
  View,
} from 'react-native';
import { ON_VOLT, Theme, fonts } from '../theme';
import { useApp } from '../state';
import { SbLogo } from '../components/SbLogo';

function field(t: Theme) {
  return {
    height: 50,
    paddingHorizontal: 14,
    backgroundColor: t.surface2,
    borderWidth: 1.5,
    borderColor: t.border,
    borderRadius: 12,
    color: t.fg1,
    fontFamily: fonts.inter500,
    fontSize: 15,
  } as const;
}

export function AuthScreen() {
  const app = useApp();
  const t = app.theme;
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const canSubmit =
    email.trim().length > 3 && password.length >= 8 && !app.authBusy;

  const submit = () => {
    if (!canSubmit) return;
    if (mode === 'login') app.login(email, password);
    else app.register(email, password, name.trim() || undefined);
  };

  return (
    <KeyboardAvoidingView
      style={{ flex: 1, backgroundColor: t.bg }}
      behavior={Platform.OS === 'ios' ? 'padding' : undefined}
    >
      <ScrollView
        contentContainerStyle={{
          flexGrow: 1,
          justifyContent: 'center',
          paddingHorizontal: 24,
          paddingVertical: 40,
          gap: 18,
        }}
        keyboardShouldPersistTaps="handled"
      >
        <View style={{ alignItems: 'center', marginBottom: 6 }}>
          <SbLogo width={200} textColor={t.fg1} />
        </View>

        <Text
          style={{
            fontFamily: fonts.grotesk600,
            fontSize: 22,
            lineHeight: 28,
            color: t.fg1,
            textAlign: 'center',
          }}
        >
          {mode === 'login' ? 'Вхід' : 'Реєстрація'}
        </Text>

        {mode === 'register' && (
          <TextInput
            value={name}
            onChangeText={setName}
            placeholder="Ім'я (необов'язково)"
            placeholderTextColor={t.fg3}
            style={field(t)}
          />
        )}
        <TextInput
          value={email}
          onChangeText={setEmail}
          placeholder="Email"
          placeholderTextColor={t.fg3}
          autoCapitalize="none"
          keyboardType="email-address"
          autoComplete="email"
          style={field(t)}
        />
        <TextInput
          value={password}
          onChangeText={setPassword}
          placeholder="Пароль (мін. 8 символів)"
          placeholderTextColor={t.fg3}
          secureTextEntry
          style={field(t)}
        />

        {!!app.authError && (
          <Text
            style={{
              fontFamily: fonts.inter500,
              fontSize: 13,
              lineHeight: 18,
              color: t.danger,
              textAlign: 'center',
            }}
          >
            {app.authError}
          </Text>
        )}

        <Pressable
          onPress={submit}
          disabled={!canSubmit}
          style={{
            height: 54,
            borderRadius: 16,
            backgroundColor: t.volt,
            opacity: canSubmit ? 1 : 0.5,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          {app.authBusy ? (
            <ActivityIndicator color={ON_VOLT} />
          ) : (
            <Text
              style={{
                fontFamily: fonts.inter700,
                fontSize: 16,
                lineHeight: 22,
                color: ON_VOLT,
              }}
            >
              {mode === 'login' ? 'Увійти' : 'Створити акаунт'}
            </Text>
          )}
        </Pressable>

        <Pressable
          onPress={() => setMode(mode === 'login' ? 'register' : 'login')}
          style={{ alignItems: 'center', paddingVertical: 6 }}
        >
          <Text
            style={{
              fontFamily: fonts.inter500,
              fontSize: 14,
              lineHeight: 20,
              color: t.fg2,
            }}
          >
            {mode === 'login'
              ? 'Немає акаунта? Зареєструватися'
              : 'Вже є акаунт? Увійти'}
          </Text>
        </Pressable>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
