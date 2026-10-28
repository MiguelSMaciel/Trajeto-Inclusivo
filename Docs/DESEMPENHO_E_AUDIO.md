# Desempenho e áudio

A versão **0.3.2** limita a renderização a **60 quadros por segundo** e desativa o sistema de áudio do Unity nos executáveis Windows e Linux. O projeto não possui músicas, efeitos, fontes de áudio ou listeners nas três cenas.

## Por que limitar os quadros

O projeto estava configurado sem VSync e sem um limite de FPS definido pelo aplicativo. A nova configuração aplica um teto de 60 FPS antes do carregamento da primeira cena, preservado ao trocar de mapa. O computador pode renderizar abaixo desse valor quando houver limitação de desempenho.

O objetivo é evitar renderização sem limite e reduzir carga desnecessária durante o uso e a apresentação. Como o ruído relatado vem fisicamente do gabinete, esse ajuste busca reduzir a carga que pode contribuir para o som. Não houve medição acústica ou diagnóstico remoto da GPU, fonte ou ventoinhas; não se pode garantir que o ruído físico desapareça.

Fabricantes descrevem ruído de alta frequência de componentes de placas de vídeo como uma possibilidade de *coil whine*. Isso é uma explicação possível, não um diagnóstico deste computador. Referência: [orientação da MSI](https://www.msi.com/faq/10183).

## Como o áudio foi desativado

O arquivo de configuração do projeto marca **Disable Unity Audio** e mantém o volume principal em zero. Segundo a [documentação do Unity](https://docs.unity.com/en-us/engine/6000.5/manual/unity-editor/editor-settings-reference/comp-manager-group/class-audio-manager), essa opção desativa o sistema de áudio nos builds standalone. O Editor tem comportamento próprio para pré-visualização de áudio.

Essa configuração atua na saída de som do aplicativo. O ruído físico do gabinete deve ser avaliado separadamente. Nenhuma configuração global de áudio, energia ou driver do computador foi alterada.

## O que permanece igual

O cálculo das rotas, as distâncias, os limites dos perfis e os dados da experiência permanecem os mesmos. A animação continua usando tempo decorrido; limitar os quadros não transforma 1× em uma velocidade diferente. Os controles de 0,5× a 8× continuam disponíveis.

Para receber o ajuste, baixe o pacote **0.3.2**, extraia toda a pasta e abra o executável dessa pasta. Os links atuais ficam em [Downloads](../Downloads/README.md).
