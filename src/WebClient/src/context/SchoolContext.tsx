import { useQuery } from '@tanstack/react-query';
import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { api, type AcademicYearDto, type CampusDto } from '../api/client';
import { useAuth } from '../auth/AuthContext';

// Año lectivo y sede con los que se trabaja (se eligen en la barra superior y se recuerdan).

interface SchoolState {
  years: AcademicYearDto[];
  campuses: CampusDto[];
  year?: AcademicYearDto;
  campus?: CampusDto;
  setYearId: (id: string) => void;
  setCampusId: (id: string) => void;
  loading: boolean;
}

const SchoolContext = createContext<SchoolState | null>(null);
const KEY = 'horarios.contexto';

function remembered(): { yearId?: string; campusId?: string } {
  try {
    return JSON.parse(localStorage.getItem(KEY) ?? '{}');
  } catch {
    return {};
  }
}

export function SchoolProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const years = useQuery({ queryKey: ['academic-years'], queryFn: () => api.catalog.academicYears.get() });
  const campuses = useQuery({ queryKey: ['campuses'], queryFn: () => api.catalog.campuses.get() });
  const [yearId, setYearId] = useState<string | undefined>(() => remembered().yearId);
  const [campusId, setCampusId] = useState<string | undefined>(() => remembered().campusId);

  useEffect(() => {
    try {
      localStorage.setItem(KEY, JSON.stringify({ yearId, campusId }));
    } catch {
      // sin almacenamiento
    }
  }, [yearId, campusId]);

  const value = useMemo<SchoolState>(() => {
    const yearList = years.data ?? [];
    const campusList = campuses.data ?? [];
    // Por defecto: el año más reciente y, para el coordinador, su sede
    const year = yearList.find((y) => y.id === yearId) ?? yearList[0];
    const campus =
      campusList.find((c) => c.id === campusId) ?? campusList.find((c) => c.id === user?.campusId) ?? campusList[0];
    return {
      years: yearList,
      campuses: campusList,
      year,
      campus,
      setYearId,
      setCampusId,
      loading: years.isLoading || campuses.isLoading
    };
  }, [years.data, campuses.data, years.isLoading, campuses.isLoading, yearId, campusId, user?.campusId]);

  return <SchoolContext.Provider value={value}>{children}</SchoolContext.Provider>;
}

export function useSchool(): SchoolState {
  const context = useContext(SchoolContext);
  if (!context) throw new Error('useSchool debe usarse dentro de SchoolProvider');
  return context;
}
