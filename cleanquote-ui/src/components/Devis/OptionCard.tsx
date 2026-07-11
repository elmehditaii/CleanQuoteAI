import type { DevisOption } from '../../types';

interface Props {
  titre: string;
  option: DevisOption;
  highlight?: boolean;
}

const euro = (n: number) =>
  n.toLocaleString('fr-FR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export default function OptionCard({ titre, option, highlight = false }: Props) {
  return (
    <div
      className={`flex flex-col rounded-xl border p-4 text-center transition ${
        highlight
          ? 'border-navy bg-navy text-white shadow-lg'
          : 'border-slate-200 bg-white text-slate-700'
      }`}
    >
      <span
        className={`text-xs font-bold uppercase tracking-wider ${
          highlight ? 'text-sky-200' : 'text-navy'
        }`}
      >
        {titre}
      </span>
      <span className="mt-2 text-lg font-bold">{euro(option.total_ttc_mensuel)} €</span>
      <span className={`text-xs ${highlight ? 'text-sky-100' : 'text-slate-400'}`}>TTC / mois</span>
      <p className={`mt-3 text-xs leading-snug ${highlight ? 'text-sky-100' : 'text-slate-500'}`}>
        {option.description}
      </p>
    </div>
  );
}
