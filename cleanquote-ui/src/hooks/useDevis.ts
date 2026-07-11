import { useCallback, useState } from 'react';
import { API_URL } from '../types';
import type { DevisData } from '../types';

interface UseDevisResult {
  devis: DevisData | null;
  devisId: string | null;
  setDevis: (devis: DevisData, devisId: string) => void;
  clearDevis: () => void;
  loadDevis: (id: string) => Promise<void>;
  downloadPdf: () => Promise<void>;
  downloading: boolean;
}

export function useDevis(): UseDevisResult {
  const [devis, setDevisState] = useState<DevisData | null>(null);
  const [devisId, setDevisId] = useState<string | null>(null);
  const [downloading, setDownloading] = useState(false);

  const setDevis = useCallback((d: DevisData, id: string) => {
    setDevisState(d);
    setDevisId(id);
  }, []);

  const clearDevis = useCallback(() => {
    setDevisState(null);
    setDevisId(null);
  }, []);

  const loadDevis = useCallback(async (id: string) => {
    const response = await fetch(`${API_URL}/api/devis/${id}`);
    if (!response.ok) return;
    const data = (await response.json()) as { id: string; contenu: { devis: DevisData } };
    setDevisState(data.contenu.devis);
    setDevisId(data.id);
  }, []);

  const downloadPdf = useCallback(async () => {
    if (!devisId) return;
    setDownloading(true);
    try {
      const response = await fetch(`${API_URL}/api/devis/${devisId}/pdf`);
      if (!response.ok) throw new Error('Erreur lors de la génération du PDF');
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${devis?.reference ?? 'devis'}.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    } finally {
      setDownloading(false);
    }
  }, [devisId, devis]);

  return { devis, devisId, setDevis, clearDevis, loadDevis, downloadPdf, downloading };
}
