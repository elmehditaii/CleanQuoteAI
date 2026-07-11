import type { DevisData } from '../../types';
import OptionCard from './OptionCard';
import PdfButton from './PdfButton';

interface Props {
  devis: DevisData | null;
  onDownloadPdf: () => void;
  downloading: boolean;
}

const euro = (n: number) =>
  n.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export default function DevisPreview({ devis, onDownloadPdf, downloading }: Props) {
  if (!devis) {
    return (
      <div className="flex h-full flex-col items-center justify-center p-8 text-center text-slate-400">
        <svg className="mb-4 h-12 w-12" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={1.5}>
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        <p className="text-sm font-medium">Aperçu du devis</p>
        <p className="mt-1 max-w-xs text-xs">
          Le devis apparaîtra ici en temps réel dès que toutes les informations auront été
          collectées dans la conversation.
        </p>
      </div>
    );
  }

  return (
    <div className="h-full space-y-5 overflow-y-auto p-5">
      <div className="rounded-xl bg-navy p-4 text-white">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold">Devis {devis.reference}</h2>
          <span className="rounded-full bg-white/15 px-3 py-1 text-xs">brouillon</span>
        </div>
        <p className="mt-1 text-sm text-sky-200">
          {devis.client.type_local} · {devis.client.superficie_m2} m² · {devis.client.adresse}
        </p>
      </div>

      <div>
        <h3 className="mb-2 text-sm font-bold uppercase tracking-wide text-navy">Prestations</h3>
        <div className="overflow-hidden rounded-xl border border-slate-200">
          <table className="w-full text-xs">
            <thead className="bg-slate-50 text-left text-slate-500">
              <tr>
                <th className="px-3 py-2 font-semibold">Prestation</th>
                <th className="px-3 py-2 font-semibold">Fréquence</th>
                <th className="px-3 py-2 text-right font-semibold">h</th>
                <th className="px-3 py-2 text-right font-semibold">HT</th>
              </tr>
            </thead>
            <tbody>
              {devis.prestations.map((p, i) => (
                <tr key={i} className="border-t border-slate-100">
                  <td className="px-3 py-2 font-medium text-slate-700">{p.nom}</td>
                  <td className="px-3 py-2 text-slate-500">{p.frequence}</td>
                  <td className="px-3 py-2 text-right text-slate-500">{p.temps_estime_h}</td>
                  <td className="px-3 py-2 text-right font-medium text-slate-700">
                    {euro(p.montant_ht)} €
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div>
        <h3 className="mb-2 text-sm font-bold uppercase tracking-wide text-navy">Vos 3 options</h3>
        <div className="grid grid-cols-3 gap-2">
          <OptionCard titre="Éco" option={devis.options.economique} />
          <OptionCard titre="Standard" option={devis.options.standard} highlight />
          <OptionCard titre="Premium" option={devis.options.premium} />
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white p-4 text-sm">
        <div className="flex justify-between py-1">
          <span className="text-slate-500">Total HT mensuel</span>
          <span className="font-semibold">{euro(devis.recapitulatif.total_ht_mensuel)} €</span>
        </div>
        <div className="flex justify-between py-1">
          <span className="text-slate-500">Total TTC mensuel</span>
          <span className="font-semibold">{euro(devis.recapitulatif.total_ttc_mensuel)} €</span>
        </div>
        <div className="mt-2 flex justify-between rounded-lg bg-navy px-3 py-2 text-white">
          <span className="font-semibold">Total TTC annuel</span>
          <span className="font-bold">{euro(devis.recapitulatif.total_ttc_annuel)} €</span>
        </div>
      </div>

      <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-xs text-slate-600">
        <p className="mb-1 font-bold uppercase tracking-wide text-navy">Comparaison marché</p>
        <p>
          Fourchette constatée : {euro(devis.comparaison_marche.prix_bas)} € –{' '}
          {euro(devis.comparaison_marche.prix_haut)} € / mois
        </p>
        <p className="mt-1">
          Notre position :{' '}
          <span className="font-semibold text-navy">{devis.comparaison_marche.notre_position}</span>
        </p>
      </div>

      <PdfButton onDownload={onDownloadPdf} downloading={downloading} />
    </div>
  );
}
