// Payment — confirm the plan, then pay on the iPay hosted page. Card details are
// entered on iPay's secure page, never in the app.
import React from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { ON_VOLT, fonts } from '../theme';
import {
  fmtDateFull,
  fmtUAH,
  monthsFromDuration,
  nextStartISO,
  planKind,
  uahFromMinor,
} from '../plans';
import { planFullLabel } from '../i18n';
import { useApp } from '../state';
import { Overline } from '../components/ui';
import { DateField } from '../components/DateField';
import { ChevronLeft, LockIcon } from '../components/icons';

export function PaymentScreen() {
  const app = useApp();
  const { theme: t, planId, payState } = app;
  const tr = app.t;
  const plan = app.plans.find((p) => p.id === planId);
  const amount = plan ? fmtUAH(uahFromMinor(plan.priceMinor)) : '';
  const planLabel = plan
    ? planFullLabel(monthsFromDuration(plan.durationDays), planKind(plan.code) === 'outdoor', tr)
    : '';
  // Earliest allowed start (stacking rule); user may pick this or later.
  const minStart = nextStartISO(app.cards);

  return (
    <ScrollView style={{ flex: 1 }} contentContainerStyle={{ paddingBottom: 100 }}>
      {/* Header */}
      <View
        style={{
          paddingTop: 60,
          paddingHorizontal: 20,
          paddingBottom: 8,
          flexDirection: 'row',
          alignItems: 'center',
          gap: 12,
        }}
      >
        <Pressable
          onPress={() => app.setScreen('plans')}
          style={{
            width: 40,
            height: 40,
            borderRadius: 16,
            backgroundColor: t.surface2,
            borderWidth: 1,
            borderColor: t.border,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <ChevronLeft size={20} color={t.fg1} />
        </Pressable>
        <Text
          style={{
            fontFamily: fonts.grotesk600,
            fontSize: 24,
            lineHeight: 32,
            letterSpacing: -0.48,
            color: t.fg1,
          }}
        >
          {tr('pay.title')}
        </Text>
      </View>

      {/* Amount due */}
      <View style={{ marginHorizontal: 20, marginTop: 14, marginBottom: 12 }}>
        <View
          style={{
            paddingVertical: 20,
            paddingHorizontal: 22,
            backgroundColor: t.bgElevated,
            borderWidth: 1,
            borderColor: t.border,
            borderRadius: 24,
          }}
        >
          <View
            style={{
              flexDirection: 'row',
              alignItems: 'flex-end',
              justifyContent: 'space-between',
            }}
          >
            <View>
              <Overline theme={t}>{tr('pay.amountDue')}</Overline>
              <Text
                style={{
                  marginTop: 4,
                  fontFamily: fonts.mono500,
                  fontSize: 44,
                  lineHeight: 48,
                  letterSpacing: -0.88,
                  color: t.fg1,
                  fontVariant: ['tabular-nums'],
                }}
              >
                {amount}
              </Text>
            </View>
            <Text
              style={{
                fontFamily: fonts.inter500,
                fontSize: 13,
                lineHeight: 18,
                color: t.fg2,
                textAlign: 'right',
                maxWidth: 130,
              }}
            >
              {planLabel}
            </Text>
          </View>

          <View style={{ marginTop: 18, height: 1, backgroundColor: t.border }} />

          <View style={{ marginTop: 14 }}>
            <Overline theme={t}>{tr('plans.startDate')}</Overline>
            <Text
              style={{
                marginTop: 4,
                fontFamily: fonts.grotesk600,
                fontSize: 26,
                lineHeight: 32,
                letterSpacing: -0.4,
                color: t.fg1,
              }}
            >
              {fmtDateFull(app.startDate, app.lang)}
            </Text>
          </View>
        </View>
      </View>

      {/* Change start date — right under the amount card */}
      <View style={{ paddingHorizontal: 20, marginBottom: 18 }}>
        <View
          style={{
            backgroundColor: t.bgElevated,
            borderWidth: 1,
            borderColor: t.border,
            borderRadius: 16,
            paddingVertical: 14,
            paddingHorizontal: 16,
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: 12,
          }}
        >
          <View style={{ flex: 1 }}>
            <Overline theme={t}>{tr('pay.changeStart')}</Overline>
            <Text
              style={{
                marginTop: 2,
                fontFamily: fonts.inter500,
                fontSize: 12,
                lineHeight: 16,
                color: t.fg3,
              }}
            >
              {tr('pay.notEarlier', { date: fmtDateFull(minStart, app.lang) })}
            </Text>
          </View>
          <DateField
            theme={t}
            value={app.startDate}
            onChange={app.setStartDate}
            minimum={minStart}
            lang={app.lang}
          />
        </View>
      </View>

      {/* Method note */}
      <View style={{ paddingHorizontal: 20 }}>
        <View
          style={{
            padding: 16,
            backgroundColor: t.surface2,
            borderWidth: 1,
            borderColor: t.border,
            borderRadius: 16,
            flexDirection: 'row',
            alignItems: 'center',
            gap: 12,
          }}
        >
          <LockIcon size={18} color={t.fg2} />
          <Text
            style={{
              flex: 1,
              fontFamily: fonts.inter500,
              fontSize: 13,
              lineHeight: 18,
              color: t.fg2,
            }}
          >
            {tr('pay.ipayNote')}
          </Text>
        </View>
      </View>

      {/* Confirm */}
      <View style={{ paddingHorizontal: 20, paddingTop: 18, paddingBottom: 28 }}>
        <Pressable
          onPress={app.confirmPayment}
          disabled={payState !== 'idle'}
          style={{
            height: 56,
            borderRadius: 24,
            backgroundColor: payState === 'success' ? t.voltDeep : t.volt,
            opacity: payState === 'processing' ? 0.85 : 1,
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 6px 20px rgba(34,224,122,0.22)',
          }}
        >
          <Text
            style={{
              fontFamily: fonts.inter600,
              fontSize: 16,
              lineHeight: 22,
              color: ON_VOLT,
            }}
          >
            {payState === 'idle' && `${tr('pay.payWithIpay')} · ${amount}`}
            {payState === 'processing' && tr('pay.processing')}
            {payState === 'success' && tr('pay.confirmed')}
          </Text>
        </Pressable>
        <View
          style={{
            marginTop: 10,
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 6,
          }}
        >
          <LockIcon size={12} color={t.fg3} />
          <Text
            style={{
              fontFamily: fonts.inter500,
              fontSize: 13,
              lineHeight: 18,
              color: t.fg3,
            }}
          >
            {tr('pay.secured')}
          </Text>
        </View>
      </View>
    </ScrollView>
  );
}
