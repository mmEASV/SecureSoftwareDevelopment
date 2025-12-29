<a id="readme-top"></a>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

<!-- PROJECT LOGO -->
<br />
<div align="center">
  <a href="https://github.com/Yet-another-solution/Template">
    <img src="images/logo.png" alt="Logo" width="80" height="80">
  </a>

<h3 align="center">project_title</h3>

  <p align="center">
    A modern .NET 9.0 Aspire application template with Blazor WebAssembly frontend, ASP.NET Core API backend, and PostgreSQL database
    <br />
    <a href="https://github.com/Yet-another-solution/Template"><strong>Explore the docs »</strong></a>
    <br />
    <br />
    <a href="https://github.com/Yet-another-solution/Template/issues/new?labels=bug&template=bug-report---.md">Report Bug</a>
    &middot;
    <a href="https://github.com/Yet-another-solution/Template/issues/new?labels=enhancement&template=feature-request---.md">Request Feature</a>
  </p>
</div>



<!-- TABLE OF CONTENTS -->
<details>
  <summary>Table of Contents</summary>
  <ol>
    <li>
      <a href="#about-the-project">About The Project</a>
      <ul>
        <li><a href="#built-with">Built With</a></li>
      </ul>
    </li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
      </ul>
    </li>
    <li><a href="#usage">Usage</a></li>
    <li><a href="#roadmap">Roadmap</a></li>
    <li><a href="#contributing">Contributing</a></li>
    <li><a href="#license">License</a></li>
    <li><a href="#contact">Contact</a></li>
    <li><a href="#acknowledgments">Acknowledgments</a></li>
  </ol>
</details>



<!-- ABOUT THE PROJECT -->
## About The Project

This is a modern .NET 9.0 Aspire application template that provides a complete full-stack solution with:

- **Blazor WebAssembly** frontend with FluentUI components
- **ASP.NET Core Web API** backend with JWT authentication
- **PostgreSQL** database with Entity Framework Core
- **Docker** containerization and orchestration via .NET Aspire
- **Automated CI/CD** pipeline with GitHub Actions

The template includes user management, authentication, validation, logging, and testing infrastructure to help you quickly bootstrap modern web applications.

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Built With

* [![.NET][DotNet]][DotNet-url]
* [![Blazor][Blazor]][Blazor-url]
* [![PostgreSQL][PostgreSQL]][PostgreSQL-url]
* [![Docker][Docker]][Docker-url]
* [![Bootstrap][Bootstrap.com]][Bootstrap-url]

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- GETTING STARTED -->
## Getting Started

To get this .NET Aspire application running locally, follow these steps:

### Prerequisites

Make sure you have the following installed:

* **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
* **Docker Desktop** - [Download here](https://www.docker.com/products/docker-desktop/)
* **Git** - [Download here](https://git-scm.com/downloads)

### Installation

1. Clone the repository
   ```sh
   git clone https://github.com/Yet-another-solution/Template.git
   cd Template
   ```

2. Restore .NET packages
   ```sh
   dotnet restore
   ```

3. Start Docker Desktop (required for PostgreSQL)

4. **Run the application using .NET Aspire**
   ```sh
   cd src/AppHost
   dotnet run
   ```

5. Access the application:
   - **Aspire Dashboard**: https://localhost:15068 (shows all services and their status)
   - **Web Application**: Check the Aspire dashboard for the assigned port
   - **API**: Check the Aspire dashboard for the assigned port

### Alternative: Running Individual Services

If you prefer to run services individually:

```sh
# Terminal 1: Start PostgreSQL
docker-compose -f src/compose.yaml up postgres

# Terminal 2: Run the API
cd src/Template.Api
dotnet run

# Terminal 3: Run the Web app
cd src/Template.Web
dotnet run
```

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- USAGE EXAMPLES -->
## Usage

### Development Commands

```sh
# Build the solution
dotnet build

# Run tests
dotnet test

# Build for production
dotnet build --configuration Release

# Database migrations (from Template.Api directory)
cd src/Template.Api
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

### Project Structure

- **src/AppHost/** - .NET Aspire orchestration project (main entry point)
- **src/Template.Api/** - ASP.NET Core Web API backend
- **src/Template.Web/** - Blazor WebAssembly frontend
- **src/Template.Shared/** - Shared models and DTOs
- **src/ServiceDefaults/** - Common Aspire service configurations
- **tests/Template.Api.Tests/** - Unit tests for the API

### Key Features

- **Authentication**: JWT-based authentication with user registration/login
- **Database**: PostgreSQL with Entity Framework Core migrations
- **UI Components**: FluentUI and Bootstrap components
- **Validation**: FluentValidation on both client and server
- **Logging**: Structured logging with Serilog
- **Testing**: xUnit tests with NSubstitute mocking
- **Containerization**: Docker support with multi-arch builds

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ROADMAP -->
## Roadmap

- [ ] Feature 1
- [ ] Feature 2
- [ ] Feature 3
    - [ ] Nested Feature

See the [open issues](https://github.com/Yet-another-solution/Template/issues) for a full list of proposed features (and known issues).

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTRIBUTING -->
## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

If you have a suggestion that would make this better, please fork the repo and create a pull request. You can also simply open an issue with the tag "enhancement".
Don't forget to give the project a star! Thanks again!

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

<p align="right">(<a href="#readme-top">back to top</a>)</p>

### Top contributors:

<a href="https://github.com/Yet-another-solution/Template/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Yet-another-solution/Template" alt="contrib.rocks image" />
</a>



<!-- LICENSE -->
## License

Distributed under the project_license. See `LICENSE.txt` for more information.

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- CONTACT -->
## Contact

Your Name - [@twitter_handle](https://twitter.com/twitter_handle) - email@email_client.com

Project Link: [https://github.com/Yet-another-solution/Template](https://github.com/Yet-another-solution/Template)

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- ACKNOWLEDGMENTS -->
## Acknowledgments

* []()
* []()
* []()

<p align="right">(<a href="#readme-top">back to top</a>)</p>



<!-- MARKDOWN LINKS & IMAGES -->
<!-- https://www.markdownguide.org/basic-syntax/#reference-style-links -->
[contributors-shield]: https://img.shields.io/github/contributors/Yet-another-solution/Template.svg?style=for-the-badge
[contributors-url]: https://github.com/Yet-another-solution/Template/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Yet-another-solution/Template.svg?style=for-the-badge
[forks-url]: https://github.com/Yet-another-solution/Template/network/members
[stars-shield]: https://img.shields.io/github/stars/Yet-another-solution/Template.svg?style=for-the-badge
[stars-url]: https://github.com/Yet-another-solution/Template/stargazers
[issues-shield]: https://img.shields.io/github/issues/Yet-another-solution/Template.svg?style=for-the-badge
[issues-url]: https://github.com/Yet-another-solution/Template/issues
[license-shield]: https://img.shields.io/github/license/Yet-another-solution/Template.svg?style=for-the-badge
[license-url]: https://github.com/Yet-another-solution/Template/blob/master/LICENSE.txt
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=for-the-badge&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/in/linkedin_username
[product-screenshot]: images/screenshot.png
[DotNet]: https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[DotNet-url]: https://dotnet.microsoft.com/
[Blazor]: https://img.shields.io/badge/Blazor-512BD4?style=for-the-badge&logo=blazor&logoColor=white
[Blazor-url]: https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor
[PostgreSQL]: https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white
[PostgreSQL-url]: https://www.postgresql.org/
[Docker]: https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white
[Docker-url]: https://www.docker.com/
[Bootstrap.com]: https://img.shields.io/badge/Bootstrap-563D7C?style=for-the-badge&logo=bootstrap&logoColor=white
[Bootstrap-url]: https://getbootstrap.com