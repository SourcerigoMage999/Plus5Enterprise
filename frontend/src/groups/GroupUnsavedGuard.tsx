import { useEffect, useRef, type RefObject } from 'react'
import { useBlocker } from 'react-router'

export function GroupUnsavedGuard({ dirty, saving, saved }: { readonly dirty: boolean; readonly saving: boolean; readonly saved: RefObject<boolean> }) {
  const blocker = useBlocker(({ currentLocation, nextLocation }) => !saved.current && (dirty || saving)
    && !nextLocation.pathname.startsWith('/auth/') && currentLocation.pathname !== nextLocation.pathname)
  const dialog = useRef<HTMLDialogElement>(null)
  useEffect(() => {
    if (blocker.state === 'blocked') dialog.current?.showModal()
    else dialog.current?.close()
  }, [blocker.state])
  useEffect(() => {
    if (!dirty && !saving) return
    const unload = (event: BeforeUnloadEvent) => { if (!saved.current) { event.preventDefault(); event.returnValue = '' } }
    window.addEventListener('beforeunload', unload)
    return () => window.removeEventListener('beforeunload', unload)
  }, [dirty, saving, saved])
  return <dialog ref={dialog} className="group-create-dialog" aria-labelledby="group-unsaved-title" onCancel={event => { event.preventDefault(); if (blocker.state === 'blocked') blocker.reset() }}>
    <h2 id="group-unsaved-title">Napustiti nespremljeni obrazac?</h2><p>{saving ? 'Spremanje je u tijeku. Pričekajte rezultat.' : 'Uneseni podaci nisu spremljeni. Napuštanjem obrasca izgubit ćete promjene.'}</p>
    <button className="ds-action ds-action--secondary" type="button" autoFocus onClick={() => { if (blocker.state === 'blocked') blocker.reset() }}>Ostani na obrascu</button>
    <button className="ds-action ds-action--danger" type="button" disabled={saving} onClick={() => { if (blocker.state === 'blocked') blocker.proceed() }}>Napusti obrazac</button>
  </dialog>
}
