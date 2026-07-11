import { useEffect, useRef, useState } from 'react';
import type { ChatMessage } from '../../types';
import MessageBubble from './MessageBubble';

interface Props {
  messages: ChatMessage[];
  loading: boolean;
  error: string | null;
  onSend: (text: string) => void;
}

export default function ChatWindow({ messages, loading, error, onSend }: Props) {
  const [input, setInput] = useState('');
  const bottomRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const submit = () => {
    if (!input.trim() || loading) return;
    onSend(input);
    setInput('');
  };

  return (
    <div className="flex h-full flex-col">
      <div className="flex-1 space-y-4 overflow-y-auto p-6">
        {messages.length === 0 && (
          <div className="flex h-full flex-col items-center justify-center text-center text-slate-500">
            <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-navy text-3xl">
              ✨
            </div>
            <h2 className="mb-2 text-xl font-semibold text-navy">Bienvenue sur CleanQuote.AI</h2>
            <p className="max-w-md text-sm">
              Décrivez votre besoin de nettoyage (type de local, superficie, fréquence,
              localisation) et je vous prépare un devis détaillé en quelques échanges.
            </p>
          </div>
        )}
        {messages.map((m) => (
          <MessageBubble key={m.id} message={m} />
        ))}
        {error && (
          <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
            {error}
          </div>
        )}
        <div ref={bottomRef} />
      </div>

      <div className="border-t border-slate-200 bg-white p-4">
        <div className="flex items-end gap-2">
          <textarea
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                submit();
              }
            }}
            rows={2}
            placeholder="Ex. : Je cherche un nettoyage pour mes bureaux de 400 m² à Lyon…"
            className="flex-1 resize-none rounded-xl border border-slate-300 px-4 py-3 text-sm focus:border-navy focus:outline-none focus:ring-2 focus:ring-navy/20"
          />
          <button
            onClick={submit}
            disabled={loading || !input.trim()}
            className="rounded-xl bg-navy px-5 py-3 text-sm font-semibold text-white transition hover:bg-navy-light disabled:cursor-not-allowed disabled:opacity-40"
          >
            {loading ? '…' : 'Envoyer'}
          </button>
        </div>
      </div>
    </div>
  );
}
