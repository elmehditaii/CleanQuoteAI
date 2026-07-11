interface Props {
  onDownload: () => void;
  downloading: boolean;
}

export default function PdfButton({ onDownload, downloading }: Props) {
  return (
    <button
      onClick={onDownload}
      disabled={downloading}
      className="flex w-full items-center justify-center gap-2 rounded-xl bg-navy px-4 py-3 text-sm font-semibold text-white transition hover:bg-navy-light disabled:opacity-50"
    >
      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
        <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v12m0 0l-4-4m4 4l4-4M4 20h16" />
      </svg>
      {downloading ? 'Génération du PDF…' : 'Télécharger le devis en PDF'}
    </button>
  );
}
