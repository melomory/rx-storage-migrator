# rx-storage-migrator

> Инструмент для миграции и управления хранилищами данных.

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](#)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Build](https://img.shields.io/badge/build-NUKE-orange)](#)
[![CI](https://github.com/melomory/rx-storage-migrator/actions/workflows/ci.yml/badge.svg)](https://github.com/melomory/rx-storage-migrator/actions/workflows/ci.yml)
[![GitHub Release](https://img.shields.io/github/v/release/melomory/rx-storage-migrator)](https://github.com/melomory/rx-storage-migrator/releases)
[![codecov](https://codecov.io/gh/melomory/rx-storage-migrator/branch/main/graph/badge.svg)](https://codecov.io/gh/melomory/rx-storage-migrator)
---

## 📌 Описание

`rx-storage-migrator` — это инструмент для выполнения миграций хранилища.

Проект ориентирован на:

- автоматизацию миграций
- повторяемые сборки
- прозрачное версионирование
- CI/CD интеграцию

---

## 🚀 Быстрый старт

### 1️⃣ Клонирование репозитория

```bash
git clone path-to-repo/rx-storage-migrator.git
cd rx-storage-migrator
```

### 2️⃣ Восстановление инструментов

Проект использует локальные dotnet tools:

```bash
dotnet tool restore
```

### 3️⃣ Сборка

```bash
dotnet nuke compile
```

### 4️⃣ Запуск тестов

```bash
dotnet nuke test
```

## 🏗 Структура проекта

```bash
src/        — исходный код
tests/      — модульные и интеграционные тесты
build/      — конфигурация сборки (NUKE)
```

Дополнительно:

- Directory.Build.props — общие настройки MSBuild
- Directory.Packages.props — Central Package Management (CPM), централизованное управление версиями пакетов
- stylecop.json — правила анализа кода
- GitVersion.yml — конфигурация автоматического версионирования
- CHANGELOG.md — история изменений

## 🛠 Требования

- .NET SDK (см. global.json)
- Git
- PowerShell или CMD (опционально)

## 🔧 Особенности использования

Для миграции данных из **Directum 5** через **SSIS** требуется предварительная подготовка базы данных.

Основные требования:

- используется **Microsoft SQL Server**;
- должно быть настроено подключение к БД **Directum 5**;
- должны быть доступны таблицы исходной системы:
  - `MBAnalit`
  - `SBEDocVer`
  - `SBEDoc`
- в БД должны быть созданы служебные таблицы, используемые мигратором.

Подробная документация:

- [Миграция из Directum 5](./docs/migration-from-directum5.md)
- [SQL-скрипт создания служебных таблиц](./docs/create-converter-tables.sql)

## 🔄 Версионирование

Проект использует GitVersion.

Версия формируется автоматически на основе git-истории и веток.
Изменять версию вручную не требуется.

## 🧪 Тестирование

Все новые изменения должны сопровождаться тестами.

Запуск:

```bash
dotnet nuke test
```

## 🎨 Стиль кода

Проект использует:

- .editorconfig
- StyleCop
- Анализаторы Roslyn

## 🤝 Вклад в проект

Перед отправкой Pull Request ознакомьтесь с:

👉 [CONTRIBUTING.md](./CONTRIBUTING.md)

## 📜 Лицензия

Проект распространяется под лицензией MIT.
См. файл [LICENSE](./LICENSE).

## 📬 Обратная связь

Если вы нашли ошибку или хотите предложить улучшение:

- создайте Issue
- опишите шаги воспроизведения
- приложите логи (при необходимости)
