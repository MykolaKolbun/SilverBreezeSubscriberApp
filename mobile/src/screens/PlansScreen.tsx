// Plans — choose a parking pass. Plans come from the backend (GET /plans).
import React from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { ON_VOLT, fonts } from '../theme';
import {
  KIND_LABEL,
  PlanKind,
  fmtDate,
  fmtUAH,
  planKind,
  uahFromMinor,
} from '../plans';
import { useApp } from '../state';
import { Overline, Radio, selectableCardStyle } from '../components/ui';
import { DateField } from '../components/DateField';
import { AddressLink } from '../components/AddressLink';
import { VENUE } from '../venue';

const KINDS: PlanKind[] = ['covered', 'outdoor'];

export function PlansScreen() {
  const app = useApp();
  const { theme: t, plans, planId } = app;
  const selected = plans.find((p) => p.id === planId);

  return (
    <ScrollView style={{ flex: 1 }} contentContainerStyle={{ paddingBottom: 100 }}>
      <View style={{ paddingTop: 60, paddingHorizontal: 20, paddingBottom: 4 }}>
        <Overline theme={t}>{VENUE.name}</Overline>
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
          Оберіть абонемент
        </Text>
        <View style={{ marginTop: 4 }}>
          <AddressLink theme={t} />
        </View>
      </View>

      <View style={{ gap: 10, paddingHorizontal: 20, paddingVertical: 16 }}>
        {plans.length === 0 && (
          <Text
            style={{
              fontFamily: fonts.inter400,
              fontSize: 14,
              lineHeight: 20,
              color: t.fg2,
            }}
          >
            Тарифи недоступні. Перевірте з’єднання й спробуйте пізніше.
          </Text>
        )}

        {KINDS.map((kind) => {
          const group = plans.filter((p) => planKind(p.code) === kind);
          if (group.length === 0) return null;
          return (
            <View key={kind} style={{ gap: 10, marginTop: 6 }}>
              <Overline theme={t}>{KIND_LABEL[kind]}</Overline>
              {group.map((plan) => {
                const active = planId === plan.id;
                // Duration lives in the name (e.g. "Паркінг · 1 місяць") — show
                // just the tail after "·" as the card title.
                const title = plan.name.includes('·')
                  ? plan.name.split('·').slice(1).join('·').trim()
                  : plan.name;
                return (
                  <Pressable
                    key={plan.id}
                    onPress={() => app.setPlanId(plan.id)}
                    style={[
                      selectableCardStyle(t, active),
                      {
                        flexDirection: 'row',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                      },
                    ]}
                  >
                    <View>
                      <Text
                        style={{
                          fontFamily: fonts.grotesk600,
                          fontSize: 18,
                          lineHeight: 24,
                          color: t.fg1,
                        }}
                      >
                        {title}
                      </Text>
                      <Text
                        style={{
                          marginTop: 4,
                          fontFamily: fonts.mono500,
                          fontSize: 26,
                          lineHeight: 30,
                          color: t.fg1,
                          fontVariant: ['tabular-nums'],
                        }}
                      >
                        {fmtUAH(uahFromMinor(plan.priceMinor))}
                      </Text>
                    </View>
                    <Radio theme={t} active={active} />
                  </Pressable>
                );
              })}
            </View>
          );
        })}

        {/* Start date */}
        {plans.length > 0 && (
          <View
            style={{
              marginTop: 10,
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
              <Overline theme={t}>Дата початку</Overline>
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
        )}
      </View>

      {/* CTA */}
      {selected && (
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
              Оформити · {fmtUAH(uahFromMinor(selected.priceMinor))}
            </Text>
          </Pressable>
        </View>
      )}
    </ScrollView>
  );
}
