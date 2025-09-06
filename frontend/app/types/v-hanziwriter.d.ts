declare module "v-hanziwriter" {
  import type HanziWriter from "hanzi-writer"

  // Component Vue wrapper
  export const Writer: any

  // Forward lại class HanziWriter gốc
  export { HanziWriter }
}
