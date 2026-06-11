<template>
  <ProjectVocabNotebook @go-back="$router.back()" />
</template>

<script setup>
definePageMeta({ layout: 'default', ssr: false, middleware: 'auth-client' })
import ProjectVocabNotebook from '~/components/ProjectVocabNotebook.vue'
</script>
