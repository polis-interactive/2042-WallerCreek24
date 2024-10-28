import { createApp } from 'vue';
import PrimeVue from 'primevue/config';
import Aura from '@primevue/themes/aura';
import ToastService from 'primevue/toastservice'; 

import 'primeicons/primeicons.css'

import App from './App.vue';

const app = createApp(App);
app.use(PrimeVue, {
    theme: {
      preset: Aura,
      options: {
        darkModeSelector: '.app-dark',
        prefix: 'p',
        cssLayer: 'primevue'
      }
    }
});
app.use(ToastService);
app.mount('#app')