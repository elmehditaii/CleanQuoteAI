import type { ConversationSummary } from '../../types';

interface Props {
  conversations: ConversationSummary[];
  activeId: string | null;
  onSelect: (conversation: ConversationSummary) => void;
  onNew: () => void;
}

export default function ConversationList({ conversations, activeId, onSelect, onNew }: Props) {
  return (
    <div className="flex h-full flex-col">
      <div className="border-b border-white/10 p-4">
        <div className="mb-4 flex items-center gap-2">
          <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-white/10 text-lg">
            🧹
          </span>
          <div>
            <h1 className="text-base font-bold text-white">CleanQuote.AI</h1>
            <p className="text-[11px] text-sky-200">Devis de nettoyage intelligent</p>
          </div>
        </div>
        <button
          onClick={onNew}
          className="w-full rounded-xl border border-white/20 bg-white/10 px-3 py-2 text-sm font-medium text-white transition hover:bg-white/20"
        >
          + Nouvelle conversation
        </button>
      </div>

      <div className="flex-1 space-y-1 overflow-y-auto p-2">
        {conversations.length === 0 && (
          <p className="p-3 text-xs text-sky-200/70">Aucune conversation pour l'instant.</p>
        )}
        {conversations.map((c) => (
          <button
            key={c.id}
            onClick={() => onSelect(c)}
            className={`w-full rounded-lg px-3 py-2.5 text-left text-xs transition ${
              c.id === activeId ? 'bg-white/15 text-white' : 'text-sky-100 hover:bg-white/10'
            }`}
          >
            <span className="line-clamp-2 leading-snug">
              {c.apercu ?? 'Conversation vide'}
            </span>
            <span className="mt-1 flex items-center gap-2 text-[10px] text-sky-300/70">
              {new Date(c.createdAt).toLocaleDateString('fr-FR')}
              <span>· {c.nbMessages} msg</span>
              {c.devisId && <span className="text-emerald-300">· devis ✓</span>}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
