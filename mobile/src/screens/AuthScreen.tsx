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
  const [step, setStep] = useState<'phone' | 'code'>('phone');
  const [phone, setPhone] = useState('+380');
  const [code, setCode] = useState('');

  const phoneOk = phone.replace(/\D/g, '').length >= 11; // +380 + 9 digits
  const codeOk = code.replace(/\D/g, '').length === 6;

  const sendCode = async () => {
    if (!phoneOk || app.authBusy) return;
    const res = await app.requestPhoneCode(phone);
    if (res.ok) {
      setStep('code');
      if (res.devCode) setCode(res.devCode); // dev autofill while SMS is stubbed
    }
  };

  const verify = () => {
    if (!codeOk || app.authBusy) return;
    app.verifyPhoneCode(phone, code);
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
          {step === 'phone' ? tr('auth.phone.title') : tr('auth.code.title')}
        </Text>

        {step === 'phone' ? (
          <>
            <Text style={hint(t)}>{tr('auth.phone.subtitle')}</Text>
            <TextInput
              value={phone}
              onChangeText={setPhone}
              placeholder="+380XXXXXXXXX"
              placeholderTextColor={t.fg3}
              keyboardType="phone-pad"
              autoComplete="tel"
              style={field(t)}
            />
          </>
        ) : (
          <>
            <Text style={hint(t)}>{tr('auth.code.sentTo', { phone })}</Text>
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
          onPress={step === 'phone' ? sendCode : verify}
          disabled={(step === 'phone' ? !phoneOk : !codeOk) || app.authBusy}
          style={{
            height: 54,
            borderRadius: 16,
            backgroundColor: t.volt,
            opacity: (step === 'phone' ? phoneOk : codeOk) && !app.authBusy ? 1 : 0.5,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          {app.authBusy ? (
            <ActivityIndicator color={ON_VOLT} />
          ) : (
            <Text style={{ fontFamily: fonts.inter700, fontSize: 16, color: ON_VOLT }}>
              {step === 'phone' ? tr('auth.phone.send') : tr('auth.code.verify')}
            </Text>
          )}
        </Pressable>

        {step === 'code' && (
          <View style={{ flexDirection: 'row', justifyContent: 'center', gap: 20 }}>
            <Pressable onPress={() => setStep('phone')} disabled={app.authBusy}>
              <Text style={link(t)}>{tr('auth.code.changeNumber')}</Text>
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
