// Pass — the digital parking pass. Shows the user's real cards + QR from the API.
import React from 'react';
import {
  ActivityIndicator,
  Image,
  Pressable,
  ScrollView,
  Text,
  View,
} from 'react-native';
import { ON_VOLT, fonts } from '../theme';
import { fmtDate, fmtUAH, todayISO, uahFromMinor } from '../plans';
import { useApp } from '../state';
import { Overline, PulseDot } from '../components/ui';
import { SbLogo } from '../components/SbLogo';
import { AddressLink } from '../components/AddressLink';
import { VENUE } from '../venue';
import { qrUrl } from '../api/client';

export function PassScreen() {
  const app = useApp();
  const { theme: t, cards, plans, cardsLoading, token } = app;

  const today = todayISO();
  const active =
    cards.find((c) => c.status === 'Active' && c.startDate <= today && today <= c.endDate) ??
    cards.find((c) => c.status === 'Active') ??
    cards[0];
  const upcoming = cards.filter((c) => c.id !== active?.id);
  const planName = (planId?: string | null) =>
    plans.find((p) => p.id === planId)?.name ?? 'Паркінг';
  const planPrice = (planId?: string | null) => {
    const p = plans.find((x) => x.id === planId);
    return p ? fmtUAH(uahFromMinor(p.priceMinor)) : '';
  };

  return (
    <ScrollView style={{ flex: 1 }} contentContainerStyle={{ paddingBottom: 110 }}>
      <View style={{ paddingTop: 58, paddingHorizontal: 20 }}>
        {/* Brand */}
        <View style={{ marginBottom: 18, gap: 6 }}>
          <SbLogo width={168} textColor={t.fg1} />
          <AddressLink theme={t} />
        </View>

        {active ? (
          <>
            <View
              style={{
                backgroundColor: t.bgElevated,
                borderWidth: 1.5,
                borderColor: t.volt,
                borderRadius: 24,
                padding: 22,
                boxShadow: '0 0 0 4px rgba(34,224,122,0.10)',
              }}
            >
              {/* QR */}
              <View style={{ alignItems: 'center' }}>
                <View
                  style={{
                    backgroundColor: '#FFFFFF',
                    borderRadius: 16,
                    padding: 14,
                    alignItems: 'center',
                    gap: 8,
                  }}
                >
                  <Image
                    source={{
                      uri: qrUrl(active.id),
                      headers: { Authorization: `Bearer ${token}` },
                    }}
                    style={{ width: 148, height: 148 }}
                    resizeMode="contain"
                  />
                  <Overline theme={t} color="#6A7187">
                    Код входу
                  </Overline>
                </View>
              </View>

              {/* Status + plan */}
              <View
                style={{
                  marginTop: 20,
                  flexDirection: 'row',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                }}
              >
                <View
                  style={{
                    flexDirection: 'row',
                    alignItems: 'center',
                    gap: 6,
                    paddingVertical: 4,
                    paddingHorizontal: 10,
                    borderRadius: 999,
                    backgroundColor: t.voltSoft,
                    borderWidth: 1,
                    borderColor: 'rgba(34,224,122,0.35)',
                  }}
                >
                  <PulseDot color={t.volt} />
                  <Text
                    style={{
                      fontFamily: fonts.inter600,
                      fontSize: 12,
                      lineHeight: 16,
                      color: t.volt,
                    }}
                  >
                    {active.status === 'Active' ? 'Активна' : active.status}
                  </Text>
                </View>
                <Text
                  style={{
                    fontFamily: fonts.grotesk600,
                    fontSize: 15,
                    lineHeight: 22,
                    color: t.fg1,
                  }}
                >
                  {planName(active.subscriptionPlanId)}
                </Text>
              </View>

              <View style={{ marginTop: 20, height: 1, backgroundColor: t.border }} />

              {/* Location / dates */}
              <View
                style={{
                  marginTop: 16,
                  flexDirection: 'row',
                  alignItems: 'flex-start',
                  justifyContent: 'space-between',
                  gap: 12,
                }}
              >
                <View style={{ flex: 1 }}>
                  <Overline theme={t}>Локація</Overline>
                  <Text
                    style={{
                      marginTop: 2,
                      fontFamily: fonts.inter600,
                      fontSize: 15,
                      lineHeight: 22,
                      color: t.fg1,
                    }}
                  >
                    {VENUE.name}
                  </Text>
                  <View style={{ marginTop: 1 }}>
                    <AddressLink theme={t} size={12} />
                  </View>
                </View>
                <View style={{ alignItems: 'flex-end' }}>
                  <Overline theme={t}>Діє до</Overline>
                  <Text
                    style={{
                      marginTop: 2,
                      fontFamily: fonts.mono500,
                      fontSize: 20,
                      lineHeight: 24,
                      color: t.fg1,
                    }}
                  >
                    {fmtDate(active.endDate)}
                  </Text>
                </View>
              </View>

              {/* Price */}
              <View
                style={{
                  marginTop: 16,
                  flexDirection: 'row',
                  alignItems: 'baseline',
                  justifyContent: 'space-between',
                }}
              >
                <Text
                  style={{
                    fontFamily: fonts.inter400,
                    fontSize: 13,
                    lineHeight: 18,
                    color: t.fg2,
                  }}
                >
                  Вартість
                </Text>
                <Text
                  style={{
                    fontFamily: fonts.mono500,
                    fontSize: 16,
                    lineHeight: 20,
                    color: t.fg1,
                  }}
                >
                  {planPrice(active.subscriptionPlanId)}
                </Text>
              </View>
            </View>

            {/* Buy another */}
            <Pressable
              onPress={app.openPlans}
              style={{
                marginTop: 16,
                height: 48,
                borderRadius: 16,
                borderWidth: 1,
                borderColor: t.borderStrong,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Text
                style={{
                  fontFamily: fonts.inter600,
                  fontSize: 14,
                  lineHeight: 22,
                  color: t.fg1,
                }}
              >
                Придбати ще
              </Text>
            </Pressable>

            {/* Upcoming */}
            {upcoming.length > 0 && (
              <View style={{ marginTop: 12, gap: 8 }}>
                <Overline theme={t}>Наступні</Overline>
                {upcoming.map((c) => (
                  <View
                    key={c.id}
                    style={{
                      minHeight: 48,
                      paddingVertical: 12,
                      paddingHorizontal: 16,
                      backgroundColor: t.bgElevated,
                      borderWidth: 1,
                      borderColor: t.border,
                      borderRadius: 16,
                      flexDirection: 'row',
                      alignItems: 'center',
                      justifyContent: 'space-between',
                      gap: 12,
                    }}
                  >
                    <Text
                      style={{
                        fontFamily: fonts.inter600,
                        fontSize: 14,
                        lineHeight: 18,
                        color: t.fg1,
                        flex: 1,
                      }}
                    >
                      {planName(c.subscriptionPlanId)}
                    </Text>
                    <Text
                      style={{
                        fontFamily: fonts.mono500,
                        fontSize: 12,
                        lineHeight: 16,
                        color: t.fg2,
                      }}
                    >
                      {fmtDate(c.startDate)} – {fmtDate(c.endDate)}
                    </Text>
                  </View>
                ))}
              </View>
            )}
          </>
        ) : cardsLoading ? (
          <View style={{ paddingVertical: 60, alignItems: 'center' }}>
            <ActivityIndicator color={t.volt} />
          </View>
        ) : (
          /* No cards yet */
          <View
            style={{
              backgroundColor: t.bgElevated,
              borderWidth: 1,
              borderColor: t.border,
              borderRadius: 24,
              padding: 24,
              alignItems: 'center',
              gap: 12,
            }}
          >
            <Text
              style={{
                fontFamily: fonts.grotesk600,
                fontSize: 18,
                lineHeight: 24,
                color: t.fg1,
                textAlign: 'center',
              }}
            >
              Немає активного абонемента
            </Text>
            <Text
              style={{
                fontFamily: fonts.inter400,
                fontSize: 14,
                lineHeight: 20,
                color: t.fg2,
                textAlign: 'center',
              }}
            >
              Придбайте абонемент на паркінг {VENUE.name}, і тут з’явиться ваш QR.
            </Text>
            <Pressable
              onPress={app.openPlans}
              style={{
                marginTop: 4,
                height: 48,
                paddingHorizontal: 24,
                borderRadius: 16,
                backgroundColor: t.volt,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Text
                style={{
                  fontFamily: fonts.inter700,
                  fontSize: 14,
                  lineHeight: 22,
                  color: ON_VOLT,
                }}
              >
                Обрати абонемент
              </Text>
            </Pressable>
          </View>
        )}
      </View>
    </ScrollView>
  );
}
