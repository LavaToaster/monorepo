package dotnet

import (
	"log/slog"
	"os"

	"github.com/bazelbuild/bazel-gazelle/language"
	"github.com/phsym/console-slog"
)

var (
	lvl    = new(slog.LevelVar)
	logger = slog.New(console.NewHandler(os.Stderr, &console.HandlerOptions{
		Level: lvl,
	}))
)

const LanguageName = "dotnet"

type dotnetLang struct {
	language.BaseLang
}

func (l *dotnetLang) Name() string {
	return LanguageName
}

func NewLanguage() language.Language {
	lvl.Set(slog.LevelInfo)

	return &dotnetLang{}
}
