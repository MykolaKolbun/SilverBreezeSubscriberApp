import React from 'react';
import { ActivityIndicator, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import { useFonts } from 'expo-font';
import {
  SpaceGrotesk_600SemiBold,
  SpaceGrotesk_700Bold,
} from '@expo-google-fonts/space-grotesk';
import {
  Inter_400Regular,
  Inter_500Medium,
  Inter_600SemiBold,
  Inter_700Bold,
} from '@expo-google-fonts/inter';
import {
  JetBrainsMono_500Medium,
  JetBrainsMono_700Bold,
} from '@expo-google-fonts/jetbrains-mono';
import { AppProvider, useApp } from './src/state';
import { BottomNav } from './src/components/BottomNav';
import { PassScreen } from './src/screens/PassScreen';
import { PlansScreen } from './src/screens/PlansScreen';
import { PaymentScreen } from './src/screens/PaymentScreen';
import { ProfileScreen } from './src/screens/ProfileScreen';
import { HistoryScreen } from './src/screens/HistoryScreen';
import { AuthScreen } from './src/screens/AuthScreen';

function Root() {
  const { theme, screen, authStatus } = useApp();

  if (authStatus === 'loading') {
    return (
      <View
        style={{
          flex: 1,
          backgroundColor: theme.bg,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <ActivityIndicator color={theme.volt} />
        <StatusBar style={theme.name === 'dark' ? 'light' : 'dark'} />
      </View>
    );
  }

  if (authStatus === 'out') {
    return (
      <View style={{ flex: 1, backgroundColor: theme.bg }}>
        <AuthScreen />
        <StatusBar style={theme.name === 'dark' ? 'light' : 'dark'} />
      </View>
    );
  }

  return (
    <View style={{ flex: 1, backgroundColor: theme.bg }}>
      {screen === 'pass' && <PassScreen />}
      {screen === 'profile' && <ProfileScreen />}
      {screen === 'plans' && <PlansScreen />}
      {screen === 'payment' && <PaymentScreen />}
      {screen === 'history' && <HistoryScreen />}
      <BottomNav />
      <StatusBar style={theme.name === 'dark' ? 'light' : 'dark'} />
    </View>
  );
}

export default function App() {
  const [fontsLoaded] = useFonts({
    SpaceGrotesk_600SemiBold,
    SpaceGrotesk_700Bold,
    Inter_400Regular,
    Inter_500Medium,
    Inter_600SemiBold,
    Inter_700Bold,
    JetBrainsMono_500Medium,
    JetBrainsMono_700Bold,
  });

  if (!fontsLoaded) return null;

  return (
    <AppProvider>
      <Root />
    </AppProvider>
  );
}
