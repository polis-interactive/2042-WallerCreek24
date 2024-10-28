import { InteractionWiFiApi } from './preload'

declare global {
    interface Window {
        InteractionWiFiApi: typeof InteractionWiFiApi
    }
}

export {}