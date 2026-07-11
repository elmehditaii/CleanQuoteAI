export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  contenu: string;
  streaming?: boolean;
}

export interface DevisClient {
  type_local: string;
  superficie_m2: number;
  adresse: string;
}

export interface Prestation {
  nom: string;
  frequence: string;
  temps_estime_h: number;
  tarif_horaire_ht: number;
  montant_ht: number;
}

export interface DevisOption {
  total_ttc_mensuel: number;
  description: string;
}

export interface DevisData {
  reference: string;
  client: DevisClient;
  prestations: Prestation[];
  options: {
    economique: DevisOption;
    standard: DevisOption;
    premium: DevisOption;
  };
  recapitulatif: {
    total_ht_mensuel: number;
    total_ttc_mensuel: number;
    total_ttc_annuel: number;
  };
  comparaison_marche: {
    prix_bas: number;
    prix_haut: number;
    notre_position: string;
  };
}

export interface ConversationSummary {
  id: string;
  sessionId: string;
  createdAt: string;
  apercu: string | null;
  nbMessages: number;
  devisId: string | null;
}

export type SseEvent =
  | { type: 'start'; conversationId: string }
  | { type: 'delta'; text: string }
  | { type: 'devis'; devisId: string; reference: string; devis: { devis: DevisData } }
  | { type: 'done'; conversationId: string }
  | { type: 'error'; message: string };

export const API_URL = 'http://localhost:5200';
