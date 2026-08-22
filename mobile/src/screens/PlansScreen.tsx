// Plans — choose a subscription plan for Harborview Garage.
import React from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { ON_VOLT, fonts } from '../theme';
import { PLANS, fmtEuro, fmtDate, periodSuffix, price } from '../plans';
import { useApp } from '../state';
import { Overline, Radio, selectableCardStyle } from '../components/ui';
import { DateField } from '../components/DateField';

export function PlansScreen() {
  const app = useApp();
  const { theme: t, billing, planId } = app;
  const suffix = periodSuffix(billing);

  return (
    <ScrollView
      style={{ flex: 1 }}
      contentContainerStyle={{ paddingBottom: 100 }}
    >
      <View style={{ paddingTop: 60, paddingHorizontal: 20, paddingBottom: 4 }}>
        <Overline theme={t}>Harborview Garage</Overline>
        <Text
          style={{
            marginTop: 4,
            fontFamily: fonts.grotesk600,
            fontSize: 24,
            lineHeight: 32,
            letterSpacing: -0.48,
            color: t.fg1,
          }}
        >
          Choose your plan
        </Text>
        <Text
          style={{
            marginTop: 4,
            fontFamily: fonts.inter400,
            fontSize: 13,
            lineHeight: 18,
            color: t.fg2,
          }}
        >
          148 Harbor St · Downtown
        </Text>
      </View>

      {/* Billing toggle */}
      <View
        style={{
          flexDirection: 'row',
          gap: 8,
          paddingHorizontal: 20,
          paddingTop: 16,
          paddingBottom: 4,
        }}
      >
        {(
          [
            ['monthly', 'Monthly'],
            ['annual', 'Annual · 2 months free'],
          ] as const
        ).map(([id, label]) => {
          const active = billing === id;
          return (
            <Pressable
              key={id}
              onPress={() => app.setBilling(id)}
              style={{
                flex: 1,
                height: 40,
                borderRadius: 12,
                alignItems: 'center',
                justifyContent: 'center',
                backgroundColor: active ? t.volt : t.bgElevated,
                borderWidth: 1,
                borderColor: active ? t.volt : t.border,
              }}
            >
              <Text
                style={{
                  fontFamily: active ? fonts.inter700 : fonts.inter500,
                  fontSize: 13,
                  lineHeight: 18,
                  color: active ? ON_VOLT : t.fg1,
                }}
              >
                {label}
              </Text>
            </Pressable>
          );
        })}
      </View>

      {/* Plan cards */}
      <View style={{ gap: 12, paddingHorizontal: 20, paddingVertical: 16 }}>
        {PLANS.map((plan) => {
          const active = planId === plan.id;
          return (
            <Pressable
              key={plan.id}
              onPress={() => app.setPlanId(plan.id)}
              style={selectableCardStyle(t, active)}
            >
              <View
                style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <View
                  style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}
                >
                  <Text
                    style={{
                      fontFamily: fonts.grotesk600,
                      fontSize: 18,
                      lineHeight: 24,
                      color: t.fg1,
                    }}
                  >
                    {plan.label}
                  </Text>
                  {plan.popular && (
                    <View
                      style={{
                        paddingHorizontal: 8,
                        paddingVertical: 3,
                        borderRadius: 999,
                        backgroundColor: t.voltSoft,
                      }}
                    >
                      <Text
                        style={{
                          fontFamily: fonts.inter600,
                          fontSize: 11,
                          lineHeight: 14,
                          color: t.volt,
                        }}
                      >
                        Most popular
                      </Text>
                    </View>
                  )}
                </View>
                <Radio theme={t} active={active} />
              </View>

              <Text style={{ marginTop: 6 }}>
                <Text
                  style={{
                    fontFamily: fonts.mono500,
                    fontSize: 32,
                    lineHeight: 36,
                    color: t.fg1,
                    fontVariant: ['tabular-nums'],
                  }}
                >
                  {fmtEuro(price(plan.id, billing))}
                </Text>
                <Text
                  style={{
                    fontFamily: fonts.inter400,
                    fontSize: 13,
                    lineHeight: 18,
                    color: t.fg3,
                  }}
                >
                  {' '}
                  {suffix}
                </Text>
              </Text>

              <View style={{ marginTop: 10, gap: 6 }}>
                {plan.features.map((f) => (
                  <Text
                    key={f}
                    style={{
                      fontFamily: fonts.inter400,
                      fontSize: 13,
                      lineHeight: 18,
                      color: t.fg2,
                    }}
                  >
                    {f}
                  </Text>
                ))}
              </View>
            </Pressable>
          );
        })}

        {/* Start date */}
        <View
          style={{
            marginTop: 4,
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
          <View>
            <Overline theme={t}>Start date</Overline>
            <Text
              style={{
                marginTop: 2,
                fontFamily: fonts.inter600,
                fontSize: 15,
                lineHeight: 22,
                color: t.fg1,
              }}
            >
              {fmtDate(app.startDate)}
            </Text>
          </View>
          <DateField theme={t} value={app.startDate} onChange={app.setStartDate} />
        </View>
      </View>

      {/* CTA */}
      <View style={{ paddingHorizontal: 20, paddingTop: 14, paddingBottom: 28 }}>
        <Pressable
          onPress={() => app.setScreen('payment')}
          style={{
            height: 56,
            borderRadius: 24,
            backgroundColor: t.volt,
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
            Subscribe · {fmtEuro(price(planId, billing))} {suffix}
          </Text>
        </Pressable>
      </View>
    </ScrollView>
  );
}
