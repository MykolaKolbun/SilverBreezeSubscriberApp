// History — the user's past transactions. Tapping a fiscalized payment opens
// the rendered fiscal receipt image (Checkbox /receipts/{id}/png, proxied).
import React, { useState } from 'react';
import {
  ActivityIndicator,
  Modal,
  Pressable,
  ScrollView,
  Text,
  View,
  useWindowDimensions,
} from 'react-native';
import { fonts } from '../theme';
import {
  fmtDate,
  fmtUAH,
  monthsFromDuration,
  planKind,
  uahFromMinor,
} from '../plans';
import { planFullLabel } from '../i18n';
import { useApp } from '../state';
import { Overline } from '../components/ui';
import { AuthImage } from '../components/AuthImage';
import { ChevronLeft, ChevronRight } from '../components/icons';
import { receiptPdfUrl, receiptUrl } from '../api/client';

export function HistoryScreen() {
  const app = useApp();
  const { theme: t, history, historyLoading, plans } = app;
  const tr = app.t;
  const { width } = useWindowDimensions();
  const [receiptId, setReceiptId] = useState<string | null>(null);

  const planName = (planId?: string | null) => {
    const p = plans.find((x) => x.id === planId);
    if (!p) return tr('kind.covered');
    return planFullLabel(monthsFromDuration(p.durationDays), planKind(p.code) === 'outdoor', tr);
  };

  const statusLabel = (s: string) => tr(`status.${s}` as any) || s;
  const statusColor = (s: string) =>
    s === 'Succeeded' ? t.volt : s === 'Pending' ? t.fg2 : t.danger;

  return (
    <ScrollView style={{ flex: 1 }} contentContainerStyle={{ paddingBottom: 110 }}>
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
          onPress={() => app.setScreen('profile')}
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
          {tr('history.title')}
        </Text>
      </View>

      {historyLoading && history.length === 0 ? (
        <View style={{ paddingVertical: 60, alignItems: 'center' }}>
          <ActivityIndicator color={t.volt} />
        </View>
      ) : history.length === 0 ? (
        <View style={{ paddingHorizontal: 20, paddingTop: 40, alignItems: 'center' }}>
          <Text
            style={{
              fontFamily: fonts.inter400,
              fontSize: 14,
              lineHeight: 20,
              color: t.fg2,
              textAlign: 'center',
            }}
          >
            {tr('history.empty')}
          </Text>
        </View>
      ) : (
        <View style={{ paddingHorizontal: 20, paddingTop: 8, gap: 10 }}>
          {history.map((p) => {
            const hasReceipt = !!p.fiscalReceiptId;
            return (
              <Pressable
                key={p.id}
                disabled={!hasReceipt}
                onPress={() => hasReceipt && setReceiptId(p.id)}
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
                  <Text
                    style={{
                      fontFamily: fonts.inter600,
                      fontSize: 15,
                      lineHeight: 22,
                      color: t.fg1,
                    }}
                  >
                    {planName(p.subscriptionPlanId)}
                  </Text>
                  <Text
                    style={{
                      marginTop: 2,
                      fontFamily: fonts.mono500,
                      fontSize: 12,
                      lineHeight: 16,
                      color: t.fg3,
                    }}
                  >
                    {fmtDate(p.updatedAt, app.lang)} ·{' '}
                    <Text style={{ color: statusColor(p.status) }}>
                      {statusLabel(p.status)}
                    </Text>
                  </Text>
                </View>
                <Text
                  style={{
                    fontFamily: fonts.mono500,
                    fontSize: 16,
                    lineHeight: 20,
                    color: t.fg1,
                  }}
                >
                  {fmtUAH(uahFromMinor(p.amountMinor))}
                </Text>
                {hasReceipt && <ChevronRight size={18} color={t.fg2} />}
              </Pressable>
            );
          })}
          <Text
            style={{
              marginTop: 4,
              fontFamily: fonts.inter400,
              fontSize: 12,
              lineHeight: 16,
              color: t.fg3,
              textAlign: 'center',
            }}
          >
            {tr('history.receiptHint')}
          </Text>
        </View>
      )}

      {/* Receipt image viewer */}
      <Modal
        visible={receiptId !== null}
        transparent
        animationType="fade"
        onRequestClose={() => setReceiptId(null)}
      >
        <View
          style={{
            flex: 1,
            backgroundColor: 'rgba(0,0,0,0.75)',
            justifyContent: 'center',
            padding: 20,
          }}
        >
          <View
            style={{
              backgroundColor: '#FFFFFF',
              borderRadius: 20,
              overflow: 'hidden',
              maxHeight: '86%',
            }}
          >
            <View
              style={{
                flexDirection: 'row',
                alignItems: 'center',
                justifyContent: 'space-between',
                paddingHorizontal: 16,
                paddingVertical: 12,
                borderBottomWidth: 1,
                borderBottomColor: '#E5E7EB',
              }}
            >
              <Overline theme={t} color="#6A7187">
                {tr('history.receiptTitle')}
              </Overline>
              <View style={{ flexDirection: 'row', alignItems: 'center', gap: 14 }}>
                <Pressable
                  onPress={() => receiptId && app.openReceiptPdf(receiptPdfUrl(receiptId))}
                  hitSlop={10}
                >
                  <Text style={{ fontFamily: fonts.inter700, fontSize: 13, color: '#009BDD' }}>
                    PDF
                  </Text>
                </Pressable>
                <Pressable onPress={() => setReceiptId(null)} hitSlop={10}>
                  <Text style={{ fontFamily: fonts.inter700, fontSize: 18, color: '#6A7187' }}>
                    ×
                  </Text>
                </Pressable>
              </View>
            </View>
            <ScrollView contentContainerStyle={{ padding: 12 }}>
              {receiptId && (
                <AuthImage
                  url={receiptUrl(receiptId)}
                  style={{ width: width - 64, height: (width - 64) * 1.9 }}
                  emptyLabel={tr('history.receiptUnavailable')}
                />
              )}
            </ScrollView>
          </View>
        </View>
      </Modal>
    </ScrollView>
  );
}
