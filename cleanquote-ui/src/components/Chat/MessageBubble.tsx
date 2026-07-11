import type { ChatMessage } from '../../types';

/** Masque les blocs JSON bruts du devis dans l'affichage du chat. */
function cleanContent(text: string): string {
  return text.replace(/```json[\s\S]*?(```|$)/g, '📋 *Devis structuré généré — voir le panneau de droite.*').trim();
}

export default function MessageBubble({ message }: { message: ChatMessage }) {
  const isUser = message.role === 'user';
  const content = isUser ? message.contenu : cleanContent(message.contenu);

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[80%] rounded-2xl px-4 py-3 text-sm leading-relaxed whitespace-pre-wrap shadow-sm ${
          isUser
            ? 'bg-navy text-white rounded-br-sm'
            : 'bg-white text-slate-800 border border-slate-200 rounded-bl-sm'
        }`}
      >
        {content || (message.streaming ? <span className="animate-pulse">▍</span> : '…')}
        {message.streaming && content && <span className="animate-pulse">▍</span>}
      </div>
    </div>
  );
}
