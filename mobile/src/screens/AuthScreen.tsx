// Auth — passwordless phone login. Step 1: enter phone → SMS code. Step 2: enter code.
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
    height: 52,
    paddingHorizontal: 14,
    backgroundColor: t.surface2,
    borderWidth: 1.5,
    borderColor: t.border,
    borderRadius: 12,
    color: t.fg1,
    fontFamily: fonts.mono500,
    fontSize: 17,
  } as const;
}

export function AuthScreen() {
  const app = useApp();
  const t = app.theme;
  const tr = app.t;
  const [step, setStep] = useState<'email' | 'code'>('email');
  const [email, setEmail] = useState('');
  const [code, setCode] = useState('');

  const emailOk = /^\S+@\S+\.\S+$/.test(email.trim());
  const codeOk = code.replace(/\D/g, '').length === 6;

  const sendCode = async () => {
    if (!emailOk || app.authBusy) return;
    const res = await app.requestEmailCode(email);
    if (res.ok) {
      setStep('code');
      if (res.devCode) setCode(res.devCode); // dev autofill while email is stubbed
    }
  };

  const verify = () => {
    if (!codeOk || app.authBusy) return;
    app.verifyEmailCode(email, code);
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
          {step === 'email' ? tr('auth.email.title') : tr('auth.code.title')}
        </Text>

        {step === 'email' ? (
          <>
            <Text style={hint(t)}>{tr('auth.email.subtitle')}</Text>
            <TextInput
              value={email}
              onChangeText={setEmail}
              placeholder="you@example.com"
              placeholderTextColor={t.fg3}
              keyboardType="email-address"
              autoCapitalize="none"
              autoComplete="email"
              style={[field(t), { fontFamily: fonts.inter500, fontSize: 15 }]}
            />
          </>
        ) : (
          <>
            <Text style={hint(t)}>{tr('auth.code.sentTo', { target: email })}</Text>
            <TextInput
              value={code}
              onChangeText={(v) => setCode(v.replace(/\D/g, '').slice(0, 6))}
              placeholder="______"
              placeholderTextColor={t.fg3}
              keyboardType="number-pad"
              autoComplete="sms-otp"
              maxLength={6}
              style={[field(t), { textAlign: 'center', letterSpacing: 8, fontSize: 22 }]}
            />
          </>
        )}

        {!!app.authError && <Text style={errorStyle(t)}>{app.authError}</Text>}

        <Pressable
          onPress={step === 'email' ? sendCode : verify}
          disabled={(step === 'email' ? !emailOk : !codeOk) || app.authBusy}
          style={{
            height: 54,
            borderRadius: 16,
            backgroundColor: t.volt,
            opacity: (step === 'email' ? emailOk : codeOk) && !app.authBusy ? 1 : 0.5,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          {app.authBusy ? (
            <ActivityIndicator color={ON_VOLT} />
          ) : (
            <Text style={{ fontFamily: fonts.inter700, fontSize: 16, color: ON_VOLT }}>
              {step === 'email' ? tr('auth.email.send') : tr('auth.code.verify')}
            </Text>
          )}
        </Pressable>

        {step === 'code' && (
          <View style={{ flexDirection: 'row', justifyContent: 'center', gap: 20 }}>
            <Pressable onPress={() => setStep('email')} disabled={app.authBusy}>
              <Text style={link(t)}>{tr('auth.code.changeTarget')}</Text>
            </Pressable>
            <Pressable onPress={sendCode} disabled={app.authBusy}>
              <Text style={link(t)}>{tr('auth.code.resend')}</Text>
            </Pressable>
          </View>
        )}
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const hint = (t: Theme) =>
  ({
    fontFamily: fonts.inter400,
    fontSize: 14,
    lineHeight: 20,
    color: t.fg2,
    textAlign: 'center',
  }) as const;

const errorStyle = (t: Theme) =>
  ({
    fontFamily: fonts.inter500,
    fontSize: 13,
    lineHeight: 18,
    color: t.danger,
    textAlign: 'center',
  }) as const;

const link = (t: Theme) =>
  ({ fontFamily: fonts.inter600, fontSize: 14, lineHeight: 20, color: t.fg2 }) as const;
