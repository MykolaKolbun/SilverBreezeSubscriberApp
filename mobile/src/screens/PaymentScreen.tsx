// Payment — pay for the selected plan. Card details are entered fresh on
// every checkout and never stored.
import React from 'react';
import { Pressable, ScrollView, Text, TextInput, View } from 'react-native';
import { ON_VOLT, Theme, fonts } from '../theme';
import { fmtDate, fmtUAH, uahFromMinor } from '../plans';
import { useApp } from '../state';
import { Overline, Radio, selectableCardStyle } from '../components/ui';
import { AppleIcon, CardIcon, ChevronLeft, LockIcon } from '../components/icons';

function CardInput({
  theme: t,
  flex,
  ...props
}: React.ComponentProps<typeof TextInput> & { theme: Theme; flex?: boolean }) {
  return (
    <TextInput
      placeholderTextColor={t.fg3}
      {...props}
      style={{
        flex: flex ? 1 : undefined,
        height: 48,
        paddingHorizontal: 14,
        backgroundColor: t.surface2,
        borderWidth: 1.5,
        borderColor: t.border,
        borderRadius: 12,
        color: t.fg1,
        fontFamily: fonts.mono500,
        fontSize: 15,
      }}
    />
  );
}

export function PaymentScreen() {
  const app = useApp();
  const { theme: t, planId, payMethod, payState } = app;
  const plan = app.plans.find((p) => p.id === planId);
  const amount = plan ? fmtUAH(uahFromMinor(plan.priceMinor)) : '';

  return (
    <ScrollView
      style={{ flex: 1 }}
      contentContainerStyle={{ paddingBottom: 100 }}
    >
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
          Payment
        </Text>
      </View>

      {/* Amount due */}
      <View style={{ marginHorizontal: 20, marginTop: 14, marginBottom: 18 }}>
        <View
          style={{
            paddingVertical: 20,
            paddingHorizontal: 22,
            backgroundColor: t.bgElevated,
            borderWidth: 1,
            borderColor: t.border,
            borderRadius: 24,
            flexDirection: 'row',
            alignItems: 'flex-end',
            justifyContent: 'space-between',
          }}
        >
          <View>
            <Overline theme={t}>Amount due</Overline>
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
          <View style={{ alignItems: 'flex-end' }}>
            <Text
              style={{
                fontFamily: fonts.inter500,
                fontSize: 13,
                lineHeight: 18,
                color: t.fg2,
                textAlign: 'right',
              }}
            >
              {plan?.name ?? ''}
            </Text>
            <Text
              style={{
                fontFamily: fonts.mono500,
                fontSize: 11,
                lineHeight: 14,
                color: t.fg3,
              }}
            >
              Starts {fmtDate(app.startDate)}
            </Text>
          </View>
        </View>
      </View>

      {/* Methods */}
      <View style={{ paddingHorizontal: 20, gap: 10 }}>
        <Overline theme={t}>Select method</Overline>

        <Pressable
          onPress={() => app.setPayMethod('applepay')}
          style={[
            selectableCardStyle(t, payMethod === 'applepay'),
            { flexDirection: 'row', alignItems: 'center', gap: 14 },
          ]}
        >
          <View
            style={{
              width: 44,
              height: 30,
              borderRadius: 6,
              backgroundColor: '#000',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <AppleIcon size={16} color="#fff" />
          </View>
          <View style={{ flex: 1 }}>
            <Text
              style={{
                fontFamily: fonts.inter600,
                fontSize: 15,
                lineHeight: 22,
                color: t.fg1,
              }}
            >
              Apple Pay
            </Text>
            <Text
              style={{
                fontFamily: fonts.mono500,
                fontSize: 11,
                lineHeight: 14,
                color: t.fg3,
              }}
            >
              Touch ID · ready
            </Text>
          </View>
          <Radio theme={t} active={payMethod === 'applepay'} />
        </Pressable>

        <Pressable
          onPress={() => app.setPayMethod('card')}
          style={selectableCardStyle(t, payMethod === 'card')}
        >
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
            <View
              style={{
                width: 44,
                height: 30,
                borderRadius: 6,
                backgroundColor: t.surface2,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <CardIcon size={18} color={t.fg2} />
            </View>
            <View style={{ flex: 1 }}>
              <Text
                style={{
                  fontFamily: fonts.inter600,
                  fontSize: 15,
                  lineHeight: 22,
                  color: t.fg1,
                }}
              >
                Credit or debit card
              </Text>
              <Text
                style={{
                  fontFamily: fonts.mono500,
                  fontSize: 11,
                  lineHeight: 14,
                  color: t.fg3,
                }}
              >
                Not saved after checkout
              </Text>
            </View>
            <Radio theme={t} active={payMethod === 'card'} />
          </View>

          {payMethod === 'card' && (
            <View style={{ marginTop: 14, gap: 10 }}>
              <CardInput
                theme={t}
                placeholder="Card number"
                keyboardType="number-pad"
                value={app.cardNumber}
                onChangeText={app.setCardNumber}
              />
              <View style={{ flexDirection: 'row', gap: 10 }}>
                <CardInput
                  theme={t}
                  flex
                  placeholder="MM/YY"
                  keyboardType="number-pad"
                  value={app.cardExpiry}
                  onChangeText={app.setCardExpiry}
                />
                <CardInput
                  theme={t}
                  flex
                  placeholder="CVC"
                  keyboardType="number-pad"
                  secureTextEntry
                  value={app.cardCvc}
                  onChangeText={app.setCardCvc}
                />
              </View>
            </View>
          )}
        </Pressable>
      </View>

      {/* Confirm */}
      <View style={{ paddingHorizontal: 20, paddingTop: 14, paddingBottom: 28 }}>
        <Pressable
          onPress={app.confirmPayment}
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
            {payState === 'idle' && `Confirm payment · ${amount}`}
            {payState === 'processing' && 'Processing…'}
            {payState === 'success' && 'Payment Confirmed!'}
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
            Secured by 256-bit SSL encryption
          </Text>
        </View>
      </View>
    </ScrollView>
  );
}
