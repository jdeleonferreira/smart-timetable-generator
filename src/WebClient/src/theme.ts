import { createTheme, type MantineColorsTuple } from '@mantine/core';

// Azul institucional (marino) y dorado de acento
const navy: MantineColorsTuple = [
  '#f2f6fb',
  '#e1e9f4',
  '#c2d2e8',
  '#9db6d9',
  '#7a9bc9',
  '#5a7fb6',
  '#3f65a0',
  '#2c5088',
  '#1b3a6b',
  '#122848'
];

const gold: MantineColorsTuple = [
  '#fbf6e9',
  '#f4ead0',
  '#e8d49f',
  '#dcbd6b',
  '#d1a940',
  '#c99d26',
  '#b08d3c',
  '#98762b',
  '#7d5f20',
  '#634a15'
];

export const theme = createTheme({
  colors: { navy, gold },
  primaryColor: 'navy',
  primaryShade: 8,
  defaultRadius: 'md',
  fontFamily: 'Inter, system-ui, -apple-system, "Segoe UI", Roboto, sans-serif',
  headings: {
    fontFamily: 'Georgia, "Times New Roman", serif',
    fontWeight: '600'
  },
  components: {
    Button: { defaultProps: { radius: 'md' }, styles: { label: { fontWeight: 600 } } },
    Paper: { defaultProps: { radius: 'md', shadow: 'xs' } },
    Table: { defaultProps: { highlightOnHover: true } },
    Modal: { defaultProps: { radius: 'md', overlayProps: { backgroundOpacity: 0.45, blur: 2 } } },
    Badge: { defaultProps: { radius: 'sm' } }
  }
});
