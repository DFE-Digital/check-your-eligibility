# Configure the Azure provider
terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.0.0"
    }
  }
  required_version = ">= 0.14.9"
}


provider "azurerm" {
  features {}
}

# Create the resource group
resource "azurerm_resource_group" "rg" {
  name     = "s203d01-core1"
  location = "westeurope"
  tags = {
      environment = "dev"
      service offering = "Eligibility Checking Service GOV.UK"
      product = "Eligibility Checking Service GOV.UK"
    }
}
