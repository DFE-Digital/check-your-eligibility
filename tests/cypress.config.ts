import { defineConfig } from "cypress";

export default defineConfig({
  e2e: {
    setupNodeEvents(on, config) {
      // implement node event listeners here
    },
    specPattern: [      
      "cypress/API/Authorisation/**.cy.ts",
      "cypress/API/Eligibility/**.cy.ts",
      "cypress/API/Application/**.cy.ts",
      "cypress/API/Schools/**.cy.ts",
      "cypress/API/**/*cy.ts",],
    baseUrl: process.env.CYPRESS_API_HOST,
    viewportWidth: 1600,
    viewportHeight: 1800,
  },
});
