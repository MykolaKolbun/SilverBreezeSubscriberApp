// Pass — the digital parking pass, default/home screen.
// Copy deliberately avoids "subscription" framing: price has no /month
// suffix, and there is no cancel flow.
import React from 'react';
import { Pressable, ScrollView, Text, View } from 'react-native';
import { fonts } from '../theme';
import { endDate, fmtDate, fmtEuro, planLabel, price } from '../plans';
import { useApp } from '../state';
import { Overline, PulseDot } from '../components/ui';
import { QrPlaceholder } from '../components/QrPlaceholder';

export function PassScreen() {
  const app = useApp();
  const { theme: t, subscriptions, vehicles } = app;

  const active = subscriptions[0];
  const upcoming = subscriptions.slice(1);
  const plate = vehicles[0]?.plate || '';

  return (
    <ScrollView
      style={{ flex: 1 }}
      contentContainerStyle={{ paddingBottom: 110 }}
    >
      <View style={{ paddingTop: 58, paddingHorizontal: 20 }}>
        {/* Hero pass card */}
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
          {/* QR block — white in both themes for scannability */}
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
              <QrPlaceholder size={132} />
              <Overline theme={t} color="#6A7187">
                Backup entry code
              </Overline>
            </View>
          </View>

          {/* Status row */}
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
                Active
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
              {planLabel(active.planId)}
            </Text>
          </View>

          {/* License plate */}
          <View style={{ marginTop: 20 }}>
            <Overline theme={t}>License plate</Overline>
          </View>
          <Text
            style={{
              marginTop: 6,
              fontFamily: fonts.mono700,
              fontSize: 40,
              lineHeight: 44,
              letterSpacing: 0.8,
              color: t.fg1,
            }}
          >
            {plate || 'No plate on file'}
          </Text>
          {!!plate && (
            <>
              <Text
                style={{
                  marginTop: 8,
                  fontFamily: fonts.inter400,
                  fontSize: 13,
                  lineHeight: 18,
                  color: t.fg2,
                }}
              >
                Recognized automatically at entry — no scan needed.
              </Text>
              {vehicles.length > 1 && (
                <Text
                  style={{
                    marginTop: 2,
                    fontFamily: fonts.inter500,
                    fontSize: 12,
                    lineHeight: 18,
                    color: t.fg3,
                  }}
                >
                  +{vehicles.length - 1} more vehicle on file · manage in Profile
                </Text>
              )}
            </>
          )}

          <View
            style={{ marginTop: 20, height: 1, backgroundColor: t.border }}
          />

          {/* Garage / End date */}
          <View
            style={{
              marginTop: 16,
              flexDirection: 'row',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}
          >
            <View>
              <Overline theme={t}>Garage</Overline>
              <Text
                style={{
                  marginTop: 2,
                  fontFamily: fonts.inter600,
                  fontSize: 15,
                  lineHeight: 22,
                  color: t.fg1,
                }}
              >
                Harborview Garage
              </Text>
            </View>
            <View style={{ alignItems: 'flex-end' }}>
              <Overline theme={t}>End date</Overline>
              <Text
                style={{
                  marginTop: 2,
                  fontFamily: fonts.mono500,
                  fontSize: 20,
                  lineHeight: 24,
                  color: t.fg1,
                }}
              >
                {fmtDate(endDate(active.startDate, active.billing))}
              </Text>
            </View>
          </View>

          {/* Price — intentionally no "/month" suffix */}
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
              Price
            </Text>
            <Text
              style={{
                fontFamily: fonts.mono500,
                fontSize: 16,
                lineHeight: 20,
                color: t.fg1,
              }}
            >
              {fmtEuro(price(active.planId, active.billing))}
            </Text>
          </View>
        </View>

        {/* Manage plan */}
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
            Manage plan
          </Text>
        </Pressable>

        {/* Upcoming (future-dated) passes */}
        {upcoming.length > 0 && (
          <View style={{ marginTop: 12, gap: 8 }}>
            <Overline theme={t}>Upcoming</Overline>
            {upcoming.map((sub) => (
              <View
                key={sub.id}
                style={{
                  height: 48,
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
                  }}
                >
                  {planLabel(sub.planId)}
                </Text>
                <Text
                  style={{
                    fontFamily: fonts.mono500,
                    fontSize: 12,
                    lineHeight: 16,
                    color: t.fg2,
                  }}
                >
                  {fmtDate(sub.startDate)} –{' '}
                  {fmtDate(endDate(sub.startDate, sub.billing))}
                </Text>
              </View>
            ))}
          </View>
        )}
      </View>
    </ScrollView>
  );
}
