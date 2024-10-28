<template>
  <section>
    <header class="header">
      <h1 class="header__text">Interaction WiFi</h1>
    </header>
    <main class="body">
      <Message :severity="stateText.serverity" :icon="stateText.icon">
        {{ stateText.text }}
      </Message>
      <Toolbar>
        <template #start>
          <FloatLabel>
            <InputText id="body__form__note" v-model="formNote" />
            <label for="body__form__note">Note</label>
          </FloatLabel>
          <FloatLabel>
            <InputNumber id="body__form__samples" v-model="formSamples" :min="1" :max="10" />
            <label for="body__form__samples">Samples</label>
          </FloatLabel>
          <FloatLabel>
            <InputNumber id="body__form__timeout" v-model="formTimeout" :min="100" :max="10000" />
            <label for="body__form__timeout">Timeout</label>
          </FloatLabel>
          <Button
            label="Take Measurement"
            severity="secondary"
            :disabled="!formCanSubmit"
            :loading="stateIsLoading"
            @click="requestMeasurement"
          ></Button>
        </template>
      </Toolbar>
      <DataTable :value="responses" showGridlines stripedRows tableStyle="min-width: 50rem">
        <Column field="note" header="Note"></Column>
        <Column field="samples" header="Samples"></Column>
        <Column field="timeout" header="Timeout"></Column>
        <Column field="rtt" header="RTT Stats"></Column>
        <Column field="ftm" header="FTM Stats"></Column>
      </DataTable>
    </main>
    <Toast position="top-right" group="tr" />
  </section>
</template>
  
<script setup lang="ts">
import { computed, Ref, ref } from 'vue';
import { InteractionEvent, InteractionSnapshot, InteractionWiFiMachineContext } from './state';
import { DISCOVERY_ADDRESS, DISCOVERY_PORT } from './peer-discovery';
import { useToast } from 'primevue/usetoast';
import Toast, { ToastMessageOptions } from 'primevue/toast';
import Message from 'primevue/message';
import Button from 'primevue/button';
import Toolbar from 'primevue/toolbar';
import InputText from 'primevue/inputtext';
import FloatLabel from 'primevue/floatlabel';
import InputNumber from 'primevue/inputnumber';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';


const toast = useToast();

const snapshot: Ref<InteractionSnapshot | null> = ref(null);
window.InteractionWiFiApi.startActor(
  (newSnapshot) => {
    snapshot.value = newSnapshot; 
  },
  (newEvent) => { 
    handleInteractionEvent(newEvent)
  }
);

const context = computed<InteractionWiFiMachineContext>(() => {
  const useSnapshot = snapshot.value;
  if (!useSnapshot || !useSnapshot.context) {
    return {
      address: null,
      responses: []
    }
  }
  return useSnapshot.context;
});

const average = (arr: Array<number>) => arr.reduce((prev, curr) => prev + curr, 0) / arr.length;
const min = (arr: Array<number>) => arr.reduce((prev, curr) => Math.min(prev, curr), 100000);
const max = (arr: Array<number>) => arr.reduce((prev, curr) => Math.max(prev, curr), -100000);

const responses = computed(() => {
  return context.value.responses.map((response) => {
    const rtt = response.reply.map(item => item.rtt);
    const ftm = response.reply.map(item => item.ftm);
    return {
      note: response.request.note,
      samples: response.request.samples,
      timeout: response.request.timeout,
      // rtt is negative
      rtt: `[${max(rtt)}, ${min(rtt)}]: ${average(rtt)}`,
      ftm: `[${min(ftm)}, ${max(ftm)}]: ${average(ftm)}`,
    }
  });
});

const state = computed(() => {
  const useSnapshot = snapshot.value;
  if (!useSnapshot || !useSnapshot.value) {
    return 'waitingForMachine';
  }
  return flattenState(useSnapshot.value);
});

const flattenState = (state: string | Record<string, unknown>): string => {
  while (typeof state !== 'string' && state !== null) {
    const entries = Object.entries(state);
    if (entries.length === 0) return 'unknown';
    const [_, subState] = entries[0];
    state = subState as string | Record<string, unknown>;
  }
  return state as string;
}

const stateText = computed<{ text: string, serverity: string, icon: string}>(() => {
  let isInfo = true;
  let text: string;
  switch (state.value) {
    case 'waitingForMachine':
      text = 'Waiting for machine to start';
      break;
    case 'waitingForServer':
      text = 'Waiting for server to start';
      break;
    case 'waitingForConnection':
      text = 'Waiting for client to connect';
      break;
    default:
      text = `Client is connected at ${context.value.address}`;
      isInfo = false;
      break;
  }
  return { text, serverity: isInfo ? 'info' : 'success', icon: 'pi ' + (isInfo ? 'pi-info-circle' : 'pi-check') };
});

const handleInteractionEvent = (event: InteractionEvent) => {
  let severity: ToastMessageOptions['severity'];
  let detail: string;
  let summary: string;
  switch (event.type) {
    case 'PEER_DISCOVERY_STARTED':
      summary = 'Peer Discovery Server';
      severity = 'info';
      detail = `Started on ${DISCOVERY_ADDRESS}:${DISCOVERY_PORT}`;
      break;
    case 'PEER_DISCOVERY_DISCOVERED':
    summary = 'Peer Discovery Server';
      severity = 'success'
      detail = `Discovered peer at ${event.address || 'UNKNOWN'}`;
      break;
    case 'PEER_DISCOVERY_WARNING':
    summary = 'Peer Discovery Server';
      severity = 'warn';
      detail = `Received warning from server : ${ event.warning || 'UNKNOWN' }`;
      break;
    case 'PEER_DISCOVERY_ERROR':
      summary = 'Peer Discovery Server';
      severity = 'error';
      detail = `Received error from server : ${ event.error || 'UNKNOWN' }`;
    default:
        return;
  }
  toast.add({
    group: 'tr',
    severity,
    summary,
    detail,
    life: 7500,
    closable: true
  })
}

const stateCanSubmit = computed(() => {
  return state.value === 'idle';
});

const stateIsLoading = computed(() => {
  return state.value === 'waitingForResponse';
});

const formNote = ref<string>('');

const formSamples = ref<number>(5);

const formTimeout = ref<number>(3000);

const formCanSubmit = computed(() => {
  return stateCanSubmit.value && formNote.value !== '' && formSamples.value > 0 && formTimeout.value > 0;
})

const requestMeasurement = () => {
  window.InteractionWiFiApi.sendMeasurementRequest({
    note: formNote.value,
    samples: formSamples.value,
    timeout: formTimeout.value
  })
};
</script>

<style>
html, body, #app {
  height: 100vh;
  width: 100vw;
  overflow: hidden;
  padding: 0;
  margin: 0;
  background-color: var(--p-surface-700);
}
.header {
  padding: 1.5rem 1rem;
  background-color: var(--p-surface-800);
}
.header__text {
  margin: 0;
}
.body {
  padding: 1.5rem 1rem;
  overflow-y: auto;
}

.p-toolbar.p-component {
  background-color: var(--p-surface-600);
  border-color: var(--p-surface-500);
  margin-top: 1rem;
  padding-top: 1.75rem;
  padding-bottom: 1rem;
}
.p-toolbar-start > :not(:first-child) {
  margin-left: 0.75rem;
}

.p-datatable {
  margin-top: 1rem;
}
</style>