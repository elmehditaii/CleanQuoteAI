import { useCallback, useRef, useState } from 'react';
import { API_URL } from '../types';
import type { ChatMessage, DevisData, SseEvent } from '../types';

function getSessionId(): string {
  let id = localStorage.getItem('cleanquote-session');
  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem('cleanquote-session', id);
  }
  return id;
}

interface UseChatResult {
  messages: ChatMessage[];
  conversationId: string | null;
  loading: boolean;
  error: string | null;
  sendMessage: (text: string) => Promise<void>;
  loadConversation: (id: string) => Promise<void>;
  newConversation: () => void;
  sessionId: string;
}

export function useChat(
  onDevis: (devis: DevisData, devisId: string) => void,
  onConversationChange?: () => void,
): UseChatResult {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const sessionId = useRef(getSessionId()).current;

  const sendMessage = useCallback(
    async (text: string) => {
      if (!text.trim() || loading) return;
      setError(null);
      setLoading(true);

      const userMsg: ChatMessage = { id: crypto.randomUUID(), role: 'user', contenu: text };
      const assistantId = crypto.randomUUID();
      setMessages((prev) => [
        ...prev,
        userMsg,
        { id: assistantId, role: 'assistant', contenu: '', streaming: true },
      ]);

      const appendDelta = (delta: string) =>
        setMessages((prev) =>
          prev.map((m) => (m.id === assistantId ? { ...m, contenu: m.contenu + delta } : m)),
        );

      try {
        const response = await fetch(`${API_URL}/api/chat`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ sessionId, conversationId, message: text }),
        });
        if (!response.ok || !response.body) {
          throw new Error(`Le serveur a répondu ${response.status}`);
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let buffer = '';

        for (;;) {
          const { done, value } = await reader.read();
          if (done) break;
          buffer += decoder.decode(value, { stream: true });

          let sep: number;
          while ((sep = buffer.indexOf('\n\n')) >= 0) {
            const raw = buffer.slice(0, sep);
            buffer = buffer.slice(sep + 2);
            const line = raw.split('\n').find((l) => l.startsWith('data: '));
            if (!line) continue;

            const event = JSON.parse(line.slice(6)) as SseEvent;
            switch (event.type) {
              case 'start':
                setConversationId(event.conversationId);
                break;
              case 'delta':
                appendDelta(event.text);
                break;
              case 'devis':
                onDevis(event.devis.devis, event.devisId);
                break;
              case 'done':
                onConversationChange?.();
                break;
              case 'error':
                setError(event.message);
                break;
            }
          }
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Erreur de connexion au serveur.');
      } finally {
        setMessages((prev) =>
          prev.map((m) => (m.id === assistantId ? { ...m, streaming: false } : m)),
        );
        setLoading(false);
      }
    },
    [conversationId, loading, onDevis, onConversationChange, sessionId],
  );

  const loadConversation = useCallback(async (id: string) => {
    const response = await fetch(`${API_URL}/api/conversations/${id}/messages`);
    if (!response.ok) return;
    const data = (await response.json()) as { id: number; role: string; contenu: string }[];
    setConversationId(id);
    setMessages(
      data.map((m) => ({
        id: String(m.id),
        role: m.role === 'user' ? 'user' : 'assistant',
        contenu: m.contenu,
      })),
    );
  }, []);

  const newConversation = useCallback(() => {
    setConversationId(null);
    setMessages([]);
    setError(null);
  }, []);

  return { messages, conversationId, loading, error, sendMessage, loadConversation, newConversation, sessionId };
}
