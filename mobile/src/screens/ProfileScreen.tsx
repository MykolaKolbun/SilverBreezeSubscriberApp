// Profile — identity, vehicles (swipeable carousel, max 3, per-card save),
// contact info, subscription shortcut, settings (theme lives here).
import React from 'react';
import {
  Alert,
  Pressable,
  ScrollView,
  Text,
  TextInput,
  View,
  useWindowDimensions,
} from 'react-native';
import { ON_VOLT, Theme, fonts } from '../theme';
import { monthsFromDuration, planKind } from '../plans';
import { planFullLabel } from '../i18n';
import { Vehicle, useApp } from '../state';
import { VENUE } from '../venue';
import { Overline } from '../components/ui';
import {
  BellIcon,
  CarIcon,
  ChevronRight,
  MailIcon,
  MoonIcon,
  PhoneIcon,
  PlusIcon,
  SignOutIcon,
  SunIcon,
} from '../components/icons';

const H_PADDING = 20;
const CARD_GAP = 12;

function fieldStyle(t: Theme) {
  return {
    height: 44,
    paddingHorizontal: 12,
    backgroundColor: t.surface2,
    borderWidth: 1.5,
    borderColor: t.border,
    borderRadius: 12,
    color: t.fg1,
    fontFamily: fonts.inter500,
    fontSize: 14,
  } as const;
}

function VehicleCard({
  theme: t,
  width,
  draft,
  saved,
}: {
  theme: Theme;
  width: number;
  draft: Vehicle;
  saved: Vehicle | undefined;
}) {
  const app = useApp();
  const tr = app.t;
  const changed =
    !saved ||
    draft.make !== saved.make ||
    draft.model !== saved.model ||
    draft.plate !== saved.plate;

  return (
    <View
      style={{
        width,
        backgroundColor: t.bgElevated,
        borderWidth: 1,
        borderColor: t.border,
        borderRadius: 16,
        paddingVertical: 16,
        paddingHorizontal: 18,
        gap: 10,
      }}
    >
      <Pressable
        onPress={() => app.removeVehicle(draft.id)}
        style={{
          position: 'absolute',
          top: 12,
          right: 12,
          width: 28,
          height: 28,
          borderRadius: 14,
          backgroundColor: t.surface2,
          borderWidth: 1,
          borderColor: t.border,
          alignItems: 'center',
          justifyContent: 'center',
          zIndex: 1,
        }}
      >
        <Text
          style={{
            fontFamily: fonts.inter700,
            fontSize: 15,
            lineHeight: 17,
            color: t.danger,
          }}
        >
          ×
        </Text>
      </Pressable>

      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 10 }}>
        <CarIcon size={20} color={t.fg2} />
        <Text
          style={{
            fontFamily: fonts.inter600,
            fontSize: 13,
            lineHeight: 18,
            color: t.fg2,
          }}
        >
          {tr('profile.vehicle')}
        </Text>
      </View>

      <View style={{ flexDirection: 'row', gap: 10 }}>
        <TextInput
          value={draft.make}
          onChangeText={(v) => app.updateDraft(draft.id, 'make', v)}
          placeholder={tr('profile.make')}
          placeholderTextColor={t.fg3}
          style={[fieldStyle(t), { flex: 1 }]}
        />
        <TextInput
          value={draft.model}
          onChangeText={(v) => app.updateDraft(draft.id, 'model', v)}
          placeholder={tr('profile.model')}
          placeholderTextColor={t.fg3}
          style={[fieldStyle(t), { flex: 1 }]}
        />
      </View>

      {/* Plate is the primary identifier — intentionally ~1.5× field size */}
      <TextInput
        value={draft.plate}
        onChangeText={(v) => app.updateDraft(draft.id, 'plate', v)}
        placeholder={tr('profile.plate')}
        placeholderTextColor={t.fg3}
        autoCapitalize="characters"
        style={[
          fieldStyle(t),
          {
            height: 56,
            paddingHorizontal: 14,
            fontFamily: fonts.mono500,
            fontSize: 23,
            textAlign: 'center',
          },
        ]}
      />

      <Pressable
        disabled={!changed}
        onPress={() => app.saveVehicle(draft.id)}
        style={{
          marginTop: 2,
          height: 40,
          borderRadius: 12,
          backgroundColor: t.volt,
          opacity: changed ? 1 : 0.4,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Text
          style={{
            fontFamily: fonts.inter700,
            fontSize: 13,
            lineHeight: 18,
            color: ON_VOLT,
          }}
        >
          {tr('profile.save')}
        </Text>
      </Pressable>
    </View>
  );
}

function SettingRow({
  theme: t,
  icon,
  label,
  labelColor,
  right,
  onPress,
}: {
  theme: Theme;
  icon: React.ReactNode;
  label: string;
  labelColor?: string;
  right?: React.ReactNode;
  onPress?: () => void;
}) {
  return (
    <Pressable
      onPress={onPress}
      disabled={!onPress}
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        gap: 14,
        paddingVertical: 14,
        paddingHorizontal: 16,
      }}
    >
      <View
        style={{
          width: 36,
          height: 36,
          borderRadius: 12,
          backgroundColor: t.surface2,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        {icon}
      </View>
      <Text
        style={{
          flex: 1,
          fontFamily: fonts.inter600,
          fontSize: 15,
          lineHeight: 22,
          color: labelColor ?? t.fg1,
        }}
      >
        {label}
      </Text>
      {right}
    </Pressable>
  );
}

function ContactRow({
  theme: t,
  icon,
  label,
  value,
}: {
  theme: Theme;
  icon: React.ReactNode;
  label: string;
  value: string;
}) {
  return (
    <View
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        gap: 14,
        paddingVertical: 14,
        paddingHorizontal: 16,
      }}
    >
      <View
        style={{
          width: 36,
          height: 36,
          borderRadius: 12,
          backgroundColor: t.surface2,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        {icon}
      </View>
      <View style={{ flex: 1 }}>
        <Text
          style={{
            fontFamily: fonts.inter500,
            fontSize: 13,
            lineHeight: 18,
            color: t.fg3,
          }}
        >
          {label}
        </Text>
        <Text
          style={{
            fontFamily: fonts.inter600,
            fontSize: 15,
            lineHeight: 22,
            color: t.fg1,
          }}
        >
          {value}
        </Text>
      </View>
    </View>
  );
}

export function ProfileScreen() {
  const app = useApp();
  const { theme: t, vehicles, drafts, cards, plans, email } = app;
  const tr = app.t;
  const activeCard = cards.find((c) => c.status === 'Active') ?? cards[0];
  const activePlan = activeCard
    ? plans.find((p) => p.id === activeCard.subscriptionPlanId)
    : undefined;
  const activePlanName = activePlan
    ? planFullLabel(
        monthsFromDuration(activePlan.durationDays),
        planKind(activePlan.code) === 'outdoor',
        tr
      )
    : null;
  const initials = (email ?? 'SB').replace(/[^a-zA-Zа-яА-ЯіїєґІЇЄҐ]/g, '').slice(0, 2).toUpperCase() || 'SB';
  const { width: screenWidth } = useWindowDimensions();
  const cardWidth = Math.min(screenWidth, 500) - H_PADDING * 2 - 20;

  const divider = (
    <View style={{ height: 1, backgroundColor: t.border, marginHorizontal: 16 }} />
  );

  return (
    <ScrollView
      style={{ flex: 1 }}
      contentContainerStyle={{ paddingBottom: 110 }}
    >
      <View style={{ paddingTop: 60, paddingHorizontal: 20, paddingBottom: 4 }}>
        <Text
          style={{
            fontFamily: fonts.grotesk600,
            fontSize: 24,
            lineHeight: 32,
            letterSpacing: -0.48,
            color: t.fg1,
          }}
        >
          {tr('profile.title')}
        </Text>
      </View>

      <View style={{ paddingTop: 16, paddingHorizontal: 20, gap: 14 }}>
        {/* Identity */}
        <View
          style={{
            backgroundColor: t.bgElevated,
            borderWidth: 1,
            borderColor: t.border,
            borderRadius: 24,
            padding: 20,
            flexDirection: 'row',
            alignItems: 'center',
            gap: 16,
          }}
        >
          <View
            style={{
              width: 64,
              height: 64,
              borderRadius: 32,
              backgroundColor: t.volt,
              alignItems: 'center',
              justifyContent: 'center',
              boxShadow: '0 8px 24px rgba(34,224,122,0.32)',
              experimental_backgroundImage:
                'linear-gradient(135deg, #22E07A, #0FB45F)',
            }}
          >
            <Text
              style={{
                fontFamily: fonts.grotesk700,
                fontSize: 20,
                color: ON_VOLT,
              }}
            >
              {initials}
            </Text>
          </View>
          <View style={{ flex: 1 }}>
            <Text
              numberOfLines={1}
              style={{
                fontFamily: fonts.inter600,
                fontSize: 16,
                lineHeight: 22,
                color: t.fg1,
              }}
            >
              {email ?? 'Акаунт'}
            </Text>
            <Text
              style={{
                marginTop: 2,
                fontFamily: fonts.inter500,
                fontSize: 13,
                lineHeight: 18,
                color: t.fg2,
              }}
            >
              {VENUE.brand}
            </Text>
          </View>
        </View>

        {/* Vehicles */}
        <View
          style={{
            marginTop: 6,
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <Overline theme={t}>{tr('profile.vehicles')}</Overline>
          <Text
            style={{
              fontFamily: fonts.inter500,
              fontSize: 12,
              lineHeight: 16,
              color: t.fg3,
            }}
          >
            {vehicles.length}/3
            {vehicles.length > 1 ? ` · ${tr('profile.swipeMore')}` : ''}
          </Text>
        </View>
      </View>

      <ScrollView
        horizontal
        showsHorizontalScrollIndicator={false}
        snapToInterval={cardWidth + CARD_GAP}
        decelerationRate="fast"
        contentContainerStyle={{
          paddingHorizontal: H_PADDING,
          gap: CARD_GAP,
          paddingBottom: 4,
        }}
        style={{ marginTop: 14 }}
      >
        {drafts.map((draft) => (
          <VehicleCard
            key={draft.id}
            theme={t}
            width={cardWidth}
            draft={draft}
            saved={vehicles.find((v) => v.id === draft.id)}
          />
        ))}
        {app.canAddVehicle && (
          <Pressable
            onPress={app.addVehicle}
            style={{
              width: cardWidth,
              minHeight: 180,
              borderRadius: 16,
              borderWidth: 1.5,
              borderStyle: 'dashed',
              borderColor: t.borderStrong,
              alignItems: 'center',
              justifyContent: 'center',
              gap: 10,
            }}
          >
            <PlusIcon size={28} color={t.fg1} />
            <Text
              style={{
                fontFamily: fonts.inter600,
                fontSize: 15,
                lineHeight: 22,
                color: t.fg1,
              }}
            >
              {tr('profile.addCar')}
            </Text>
          </Pressable>
        )}
      </ScrollView>

      <View style={{ paddingHorizontal: 20, paddingTop: 10, gap: 14 }}>
        <Text
          style={{
            fontFamily: fonts.inter400,
            fontSize: 12,
            lineHeight: 16,
            color: t.fg3,
          }}
        >
          {tr('profile.plateNote', { name: VENUE.name })}
        </Text>

        {/* Contact info */}
        <View
          style={{
            backgroundColor: t.bgElevated,
            borderWidth: 1,
            borderColor: t.border,
            borderRadius: 16,
            overflow: 'hidden',
          }}
        >
          <ContactRow
            theme={t}
            icon={<PhoneIcon size={18} color={t.fg2} />}
            label={tr('profile.phone')}
            value={tr('profile.notSet')}
          />
          {divider}
          <ContactRow
            theme={t}
            icon={<MailIcon size={18} color={t.fg2} />}
            label={tr('profile.email')}
            value={email ?? tr('profile.notSet')}
          />
        </View>

        {/* Subscription shortcut */}
        <Pressable
          onPress={() => app.setScreen('pass')}
          style={{
            backgroundColor: t.voltSoft,
            borderWidth: 1,
            borderColor: 'rgba(34,224,122,0.3)',
            borderRadius: 16,
            paddingVertical: 14,
            paddingHorizontal: 16,
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <View>
            <Overline theme={t} color={t.volt}>
              {tr('profile.subscription')}
            </Overline>
            <Text
              style={{
                marginTop: 2,
                fontFamily: fonts.inter600,
                fontSize: 15,
                lineHeight: 22,
                color: t.fg1,
              }}
            >
              {activePlanName
                ? `${activePlanName} · ${VENUE.name}`
                : tr('profile.noActiveSub')}
            </Text>
          </View>
          <ChevronRight size={18} color={t.fg1} />
        </Pressable>

        {/* Settings */}
        <View style={{ marginTop: 6 }}>
          <Overline theme={t}>{tr('profile.settings')}</Overline>
        </View>
        <View
          style={{
            backgroundColor: t.bgElevated,
            borderWidth: 1,
            borderColor: t.border,
            borderRadius: 16,
            overflow: 'hidden',
          }}
        >
          <SettingRow
            theme={t}
            icon={<BellIcon size={18} color={t.fg2} />}
            label={tr('profile.notifications')}
            right={
              <Pressable
                onPress={app.toggleNotifications}
                style={{
                  width: 52,
                  height: 32,
                  borderRadius: 999,
                  backgroundColor: app.notifications ? t.volt : t.surface2,
                }}
              >
                <View
                  style={{
                    position: 'absolute',
                    top: 2,
                    left: app.notifications ? 22 : 2,
                    width: 24,
                    height: 24,
                    borderRadius: 12,
                    backgroundColor: app.notifications ? ON_VOLT : t.fg2,
                  }}
                />
              </Pressable>
            }
          />
          {divider}
          <SettingRow
            theme={t}
            icon={<SunIcon size={18} color={t.fg2} />}
            label={tr('profile.appearance')}
            right={
              <View
                style={{
                  flexDirection: 'row',
                  height: 36,
                  borderRadius: 999,
                  backgroundColor: t.bg,
                  borderWidth: 1,
                  borderColor: t.border,
                  padding: 2,
                  gap: 2,
                }}
              >
                <Pressable
                  onPress={() => app.setThemeName('light')}
                  style={{
                    width: 36,
                    height: 30,
                    borderRadius: 15,
                    alignItems: 'center',
                    justifyContent: 'center',
                    backgroundColor:
                      t.name === 'light' ? 'rgba(255,210,122,0.18)' : 'transparent',
                  }}
                >
                  <SunIcon
                    size={16}
                    color={t.name === 'light' ? '#FFD27A' : '#8A92A4'}
                  />
                </Pressable>
                <Pressable
                  onPress={() => app.setThemeName('dark')}
                  style={{
                    width: 36,
                    height: 30,
                    borderRadius: 15,
                    alignItems: 'center',
                    justifyContent: 'center',
                    backgroundColor:
                      t.name === 'dark' ? t.voltSoft : 'transparent',
                  }}
                >
                  <MoonIcon
                    size={16}
                    color={t.name === 'dark' ? t.volt : '#8A92A4'}
                  />
                </Pressable>
              </View>
            }
          />
          {divider}
          <SettingRow
            theme={t}
            icon={
              <Text
                style={{
                  fontFamily: fonts.inter700,
                  fontSize: 12,
                  color: t.fg2,
                }}
              >
                {app.lang.toUpperCase()}
              </Text>
            }
            label={tr('profile.language')}
            right={
              <View
                style={{
                  flexDirection: 'row',
                  height: 36,
                  borderRadius: 999,
                  backgroundColor: t.bg,
                  borderWidth: 1,
                  borderColor: t.border,
                  padding: 2,
                  gap: 2,
                }}
              >
                {(['uk', 'en'] as const).map((l) => (
                  <Pressable
                    key={l}
                    onPress={() => app.setLang(l)}
                    style={{
                      paddingHorizontal: 12,
                      height: 30,
                      borderRadius: 15,
                      alignItems: 'center',
                      justifyContent: 'center',
                      backgroundColor: app.lang === l ? t.voltSoft : 'transparent',
                    }}
                  >
                    <Text
                      style={{
                        fontFamily: fonts.inter600,
                        fontSize: 13,
                        color: app.lang === l ? t.volt : t.fg2,
                      }}
                    >
                      {l.toUpperCase()}
                    </Text>
                  </Pressable>
                ))}
              </View>
            }
          />
          {divider}
          <SettingRow
            theme={t}
            icon={<SignOutIcon size={18} color={t.danger} />}
            label={tr('profile.signOut')}
            labelColor={t.danger}
            onPress={() =>
              Alert.alert(tr('profile.signOut'), tr('profile.signOut.confirmBody'), [
                { text: tr('common.cancel'), style: 'cancel' },
                {
                  text: tr('profile.signOut'),
                  style: 'destructive',
                  onPress: () => {
                    app.logout();
                  },
                },
              ])
            }
          />
        </View>

        <Text
          style={{
            textAlign: 'center',
            fontFamily: fonts.mono500,
            fontSize: 11,
            lineHeight: 14,
            color: t.fg3,
            paddingTop: 8,
            paddingBottom: 4,
          }}
        >
          {VENUE.brand} v1.0
        </Text>
      </View>
    </ScrollView>
  );
}
