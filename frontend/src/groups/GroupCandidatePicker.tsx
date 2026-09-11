import { useState } from 'react'
import { useGroupResource, type Page } from './groupsApi.ts'

export interface CreationCandidate { id: string; firstName: string; lastName: string; schoolGrade: string; programName: string | null; recommended: boolean; rowVersion: string }

export function GroupCandidatePicker({ programId, gradeId, capacity, selected, onChange }: {
  readonly programId: string; readonly gradeId: string; readonly capacity: number
  readonly selected: CreationCandidate[]; readonly onChange: (value: CreationCandidate[]) => void
}) {
  const [search, setSearch] = useState('')
  const [paging, setPaging] = useState({ programId, gradeId, search, page: 1 })
  const [revision, setRevision] = useState(0)
  const page = paging.programId === programId && paging.gradeId === gradeId && paging.search === search ? paging.page : 1
  const params = new URLSearchParams({ programId, schoolGradeId: gradeId, search, page: String(page), pageSize: '8' })
  const resource = useGroupResource<Page<CreationCandidate>>(`/groups/create-candidates?${params}`, revision)
  const full = !Number.isInteger(capacity) || capacity < 1 || selected.length >= capacity || selected.length >= 100
  return <div className="group-create-picker">
    <p>Prikazuju se vaši učenici bez aktivne grupe. Odgovarajući razred ima prednost. Spremanjem učenici preuzimaju program grupe i grupni način rada.</p>
    <label className="group-create-field"><span>Pretraži učenike</span><input type="search" maxLength={100} value={search} onChange={event => setSearch(event.target.value)} placeholder="Pretraži učenike…" /></label>
    {resource?.error ? <div role="alert">Učenike nije moguće učitati. <button type="button" onClick={() => setRevision(value => value + 1)}>Pokušaj ponovno</button></div> : !resource?.data ? <p role="status">Učitavanje učenika…</p> : <>
      {resource.data.items.length === 0 && <p>Nema dostupnih učenika za ovu pretragu.</p>}
      <ul className="group-create-candidates">{resource.data.items.map(student => {
        const checked = selected.some(item => item.id === student.id)
        return <li key={student.id}><label><input type="checkbox" checked={checked} disabled={!checked && full} onChange={() => onChange(checked ? selected.filter(item => item.id !== student.id) : [...selected, student])} /><span><strong>{student.firstName} {student.lastName}</strong><small>{student.schoolGrade} · {student.programName ?? 'Bez programa'}</small>{student.recommended && <small>Odgovara programu i razredu</small>}</span></label></li>
      })}</ul>
      <nav className="group-create-paging" aria-label="Stranice učenika"><button type="button" disabled={page <= 1} onClick={() => setPaging({ programId, gradeId, search, page: page - 1 })}>Prethodna</button><span>{page} / {Math.max(1, resource.data.totalPages)}</span><button type="button" disabled={page >= resource.data.totalPages} onClick={() => setPaging({ programId, gradeId, search, page: page + 1 })}>Sljedeća</button></nav>
    </>}
    {full && <p role="status">{selected.length >= 100 ? 'U jednom zahtjevu možete odabrati do 100 učenika. Ostale dodajte nakon stvaranja grupe.' : capacity > 0 ? 'Dosegnut je kapacitet. Povećajte ga za dodatni odabir.' : 'Unesite pozitivan cijeli kapacitet prije odabira učenika.'}</p>}
    <div className="group-create-selection"><strong>Odabrano {selected.length} učenika</strong><button type="button" disabled={!selected.length} onClick={() => onChange([])}>Očisti odabir</button></div>
    {selected.length > 0 && <ul aria-label="Odabrani učenici">{selected.map(student => <li key={student.id}>{student.firstName} {student.lastName} <button type="button" aria-label={`Ukloni odabir ${student.firstName} ${student.lastName}`} onClick={() => onChange(selected.filter(item => item.id !== student.id))}>Ukloni</button></li>)}</ul>}
  </div>
}
