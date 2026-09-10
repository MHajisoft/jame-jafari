<script setup>
import { computed } from 'vue'
import PrefIconLabelCell from './PrefIconLabelCell.vue'
import MessengerKindIcon from './MessengerKindIcon.vue'
import { messengerKindLabel } from '../utils/enums'

const props = defineProps({
  kind: { type: [Number, String], default: null },
  label: { type: String, default: '' },
  size: { type: [Number, String], default: 18 },
  emptyLabel: { type: String, default: '—' }
})

const text = computed(() => props.label || messengerKindLabel(props.kind) || '')
const hasIcon = computed(() => props.kind != null && props.kind !== '')
</script>

<template>
  <PrefIconLabelCell :label="text" :size="size" :empty-label="emptyLabel">
    <template v-if="hasIcon" #icon="{ size: iconSize }">
      <MessengerKindIcon :kind="kind" :size="iconSize" :title="text" />
    </template>
  </PrefIconLabelCell>
</template>
