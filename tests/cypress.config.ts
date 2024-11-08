import { defineConfig } from "cypress";
import { faker } from '@faker-js/faker'

export default defineConfig({
  e2e: {
    setupNodeEvents(on, config) {
      // implement node event listeners here

      const generatedLastName = faker.person.lastName().toUpperCase();
      config.env.lastName = generatedLastName;
      return config;
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
    reporter: "junit",
    reporterOptions: {
        mochaFile: "results/my-test-output-[hash].xml",
    },
    retries: {
      "runMode": 1,
      "openMode": 1
    }
});
