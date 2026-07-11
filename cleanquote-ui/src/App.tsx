import { useCallback, useEffect, useState } from 'react';
import ChatWindow from './components/Chat/ChatWindow';
import DevisPreview from './components/Devis/DevisPreview';
import ConversationList from './components/Sidebar/ConversationList';
import { useChat } from './hooks/useChat';
import { useDevis } from './hooks/useDevis';
import { API_URL } from './types';
import type { ConversationSummary } from './types';

export default function App() {
  const { devis, setDevis, clearDevis, loadDevis, downloadPdf, downloading } = useDevis();
  const [conversations, setConversations] = useState<ConversationSummary[]>([]);

  const refreshConversations = useCallback(async () => {
    const sessionId = localStorage.getItem('cleanquote-session');
    if (!sessionId) return;
    try {
      const response = await fetch(`${API_URL}/api/conversations?sessionId=${sessionId}`);
      if (response.ok) setConversations(await response.json());
    } catch {
      // API hors ligne : la sidebar restera vide
    }
  }, []);

  const chat = useChat(setDevis, refreshConversations);

  useEffect(() => {
    refreshConversations();
  }, [refreshConversations]);

  const selectConversation = async (c: ConversationSummary) => {
    clearDevis();
    await chat.loadConversation(c.id);
    if (c.devisId) await loadDevis(c.devisId);
  };

  const newConversation = () => {
    chat.newConversation();
    clearDevis();
  };

  return (
    <div className="flex h-screen bg-slate-100 font-sans text-slate-900">
      {/* Sidebar gauche : historique */}
      <aside className="w-64 shrink-0 bg-navy">
        <ConversationList
          conversations={conversations}
          activeId={chat.conversationId}
          onSelect={selectConversation}
          onNew={newConversation}
        />
      </aside>

      {/* Centre : chat */}
      <main className="flex min-w-0 flex-1 flex-col border-r border-slate-200">
        <ChatWindow
          messages={chat.messages}
          loading={chat.loading}
          error={chat.error}
          onSend={chat.sendMessage}
        />
      </main>

      {/* Panneau droit : aperçu du devis */}
      <aside className="w-[420px] shrink-0 bg-slate-50">
        <DevisPreview devis={devis} onDownloadPdf={downloadPdf} downloading={downloading} />
      </aside>
    </div>
  );
}
