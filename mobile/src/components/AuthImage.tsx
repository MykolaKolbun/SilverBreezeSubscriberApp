// Loads an authenticated image (e.g. the fiscal receipt PNG) via fetch with the
// JWT — refreshing the token on 401 — and renders it from a data URI. Avoids the
// unreliable <Image source={{ headers }}> path in Android release builds.
import React, { useEffect, useState } from 'react';
import { ActivityIndicator, Image, ImageStyle, StyleProp, Text, View } from 'react-native';
import { fonts } from '../theme';
import { useApp } from '../state';

export function AuthImage({
  url,
  style,
  emptyLabel,
}: {
  url: string;
  style: StyleProp<ImageStyle>;
  emptyLabel?: string;
}) {
  const app = useApp();
  const t = app.theme;
  const [uri, setUri] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setUri(null);
    setFailed(false);
    (async () => {
      try {
        const data = await app.fetchImage(url);
        if (!cancelled) {
          if (data) setUri(data);
          else setFailed(true);
        }
      } catch {
        if (!cancelled) setFailed(true);
      }
    })();
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [url]);

  if (!failed && uri)
    return (
      <Image
        source={{ uri }}
        style={style}
        resizeMode="contain"
        onError={() => setFailed(true)}
      />
    );

  return (
    <View style={[style as object, { alignItems: 'center', justifyContent: 'center' }]}>
      {failed ? (
        <Text
          style={{
            fontFamily: fonts.inter500,
            fontSize: 13,
            color: t.fg3,
            textAlign: 'center',
          }}
        >
          {emptyLabel ?? '—'}
        </Text>
      ) : (
        <ActivityIndicator color={t.volt} />
      )}
    </View>
  );
}
