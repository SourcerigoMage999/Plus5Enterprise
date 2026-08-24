import { Link } from 'react-router'

interface FoundationPageProps {
  readonly title: string
}

export function FoundationPage({ title }: FoundationPageProps) {
  return (
    <section className="foundation-page" aria-labelledby="page-title">
      <div className="foundation-page__card">
        <p className="foundation-page__eyebrow">Aplikacijski temelj</p>
        <h1 id="page-title">{title}</h1>
        <p className="foundation-page__description">
          Navigacija i zajednički vizualni temelj su spremni. Sadržaj ovog modula bit će
          uveden u njegovoj odobrenoj ROADMAP fazi.
        </p>
        <p className="foundation-page__status">Bez lažnih podataka i nedokumentiranih funkcija.</p>
      </div>
    </section>
  )
}

export function NotFoundPage() {
  return (
    <section className="foundation-page" aria-labelledby="page-title">
      <div className="foundation-page__card">
        <p className="foundation-page__eyebrow">404</p>
        <h1 id="page-title">Stranica nije pronađena</h1>
        <p className="foundation-page__description">
          Tražena adresa nije dio trenutačno definirane PLUS 5 navigacije.
        </p>
        <Link className="foundation-page__action" to="/">
          Povratak na Radni stol
        </Link>
      </div>
    </section>
  )
}
