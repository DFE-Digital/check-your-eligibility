terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 2.65"
    }

    random = {
      source  = "hashicorp/random"
      version = "3.1.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "patricksdemostorage"
    storage_account_name = "patricksdemostorage"
    container_name       = "microservice"
    key                  = "microservice.state"
  }
}

provider "azurerm" {
  features {}
}
